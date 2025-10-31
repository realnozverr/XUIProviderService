namespace VpnProviderWorker.Kafka;

public class KafkaOptions
{
    public required string BootstrapServers { get; init; }
    public required string GroupId { get; init; }
}