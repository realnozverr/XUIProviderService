namespace VpnProviderWorker.Persistence.Outbox;

public class OutboxEvent
{
    public required Guid EventId { get; init; }
    public required string Type { get; init; }
    public required string Content { get; init; }
    public required DateTime OccurredOnUtc { get; init; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; } = null;
    
    public void MarkProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
    }
    public void MarkFailed(string error)
    {
        Error = error;
    }

}