using Newtonsoft.Json;
using VpnProviderWorker.Persistence.Inbox.InputConsumerEvents;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage;

namespace VpnProviderWorker.Persistence.Inbox;

public class Inbox(IDbContextFactory<DataContext> dataContext) : IInbox
{
    private const string DuplicateKeyCode = "19";
    private readonly JsonSerializerSettings _jsonSettings = new() { TypeNameHandling = TypeNameHandling.All };

    public async Task<bool> Save(IConvertibleToCommand consumerEvent)
    {
        await using var context = await dataContext.CreateDbContextAsync();
        var connection = context.Database.GetDbConnection();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await connection.ExecuteAsync(Sql, new
            {
                EventId = consumerEvent.EventId,
                Type = consumerEvent.GetType().Name,
                Content = JsonConvert.SerializeObject(consumerEvent, _jsonSettings),
                OccurredOnUtc = DateTime.UtcNow
            }, transaction.GetDbTransaction());

            await transaction.CommitAsync();
        }
        catch (SqliteException e) when (e is { SqlState: DuplicateKeyCode })
        {
            return true;
        }
        catch
        {
            return false;
        }
        return true;
    }

    private const string Sql =
        """
        INSERT INTO inbox (event_id, type, content, occurred_on_utc)
        VALUES (@EventId, @Type, @Content, @OccurredOnUtc)
        """;
}