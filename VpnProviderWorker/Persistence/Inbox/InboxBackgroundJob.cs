using System.Collections.Concurrent;
using System.Data;
using Dapper;
using JsonNet.ContractResolvers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using Quartz;
using VpnProviderWorker.Persistence.Inbox.InputConsumerEvents;

namespace VpnProviderWorker.Persistence.Inbox;

[DisallowConcurrentExecution]
public class InboxBackgroundJob(
    IDbContextFactory<DataContext> dbContextFactory,
    IMediator mediator,
    ILogger<InboxBackgroundJob> logger) : IJob
{
    private readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        ContractResolver = new PrivateSetterAndCtorContractResolver()
    };

    public async Task Execute(IJobExecutionContext jobExecutionContext)
    {
        var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var dapperTransaction = transaction.GetDbTransaction();
        await using var connection = dbContext.Database.GetDbConnection();
        var inboxEvents = (await connection.QueryAsync<InboxEvent>(QuerySql, dapperTransaction))
            .ToList()
            .AsReadOnly();

        if (inboxEvents.Count > 0)
        {
            var updateQueue = new ConcurrentQueue<EventUpdate>();

            var consumerEvents = inboxEvents
                .Select(ev =>
                    JsonConvert.DeserializeObject<IConvertibleToCommand>(ev.Content,
                        _jsonSerializerSettings))
                .OfType<IConvertibleToCommand>()
                .AsList()
                .AsReadOnly();
            
            var sendTasks = consumerEvents
                .Select(domainEvent => SendToMediator(domainEvent, updateQueue, 
                    jobExecutionContext.CancellationToken))
                .ToList()
                .AsReadOnly();
            
            await Task.WhenAll(sendTasks);

            while (updateQueue.TryDequeue(out var update))
            {
                await connection.ExecuteAsync(
                    "UPDATE inbox SET processed_on_utc = @ProcessedOnUtc WHERE event_id = @EventId",
                    new { update.ProcessedOnUtc, update.EventId },
                    transaction: dapperTransaction);
            }

            await transaction.CommitAsync();
        }
        
        return;

        async Task SendToMediator(
            IConvertibleToCommand @event,
            ConcurrentQueue<EventUpdate> updateQueue,
            CancellationToken cancellationToken)
        {
            try
            {
                var processingResult = await mediator.Send(@event.ToCommand(), cancellationToken);
                if (processingResult.IsSuccess)
                    updateQueue.Enqueue(new EventUpdate(@event.EventId, DateTime.UtcNow));
            }
            catch (Exception e)
            {
                updateQueue.Enqueue(new EventUpdate(@event.EventId));
                logger.LogError("Fail in processing inbox events, exception: {e}", e);
            }
        }
    }


    private class EventUpdate(Guid eventId, DateTime? processedOnUtc = null)
    {
        public Guid EventId { get; } = eventId;
        public DateTime? ProcessedOnUtc { get; } = processedOnUtc;
    }

    private const string QuerySql =
        """
        SELECT event_id AS EventId, 
               content AS Content
        FROM inbox
        WHERE processed_on_utc IS NULL
        ORDER BY occurred_on_utc
        LIMIT 50
        """;
}