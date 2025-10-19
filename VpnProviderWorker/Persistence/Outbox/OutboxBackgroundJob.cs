using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;

namespace VpnProviderWorker.Persistence.Outbox;

[DisallowConcurrentExecution]
public sealed class OutboxBackgroundJob : IJob
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxBackgroundJob> _logger;
    private readonly JsonSerializerSettings _jsonSettings;

    public OutboxBackgroundJob(
        IDbContextFactory<DataContext> contextFactory,
        IPublisher publisher,
        ILogger<OutboxBackgroundJob> logger)
    {
        _contextFactory = contextFactory;
        _publisher = publisher;
        _logger = logger;
        
        _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);
        
        var outboxMessages = await GetPendingMessagesAsync(dbContext, context.CancellationToken);
        
        if (outboxMessages.Count == 0) return;

        foreach (var message in outboxMessages)
        {
            try
            {
                var domainEvent = JsonConvert.DeserializeObject(message.Content, _jsonSettings);
                if (domainEvent is null)
                {
                    throw new JsonException($"Failed to deserialize event content for message {message.EventId}");
                }
                
                await _publisher.Publish(domainEvent, context.CancellationToken);
                
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.EventId);
                message.MarkFailed(ex.ToString());
            }
        }

        // Поскольку GetPendingMessagesAsync использует AsNoTracking(),
        // нам нужно явно указать контексту, что эти сущности были изменены.
        dbContext.UpdateRange(outboxMessages);
        
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private static async Task<List<OutboxEvent>> GetPendingMessagesAsync(
        DataContext dbContext,
        CancellationToken ct)
    {
        const int batchSize = 50;
        
        return await dbContext.Outbox
            .FromSqlRaw($@"
                SELECT * FROM ""outbox""
                WHERE ""processed_on_utc"" IS NULL
                ORDER BY ""occurred_on_utc""
                LIMIT {batchSize}")
            .AsNoTracking()
            .ToListAsync(ct);
    }
}