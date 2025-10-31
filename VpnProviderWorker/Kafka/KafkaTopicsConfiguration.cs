namespace VpnProviderWorker.Kafka;

public record KafkaTopicsConfiguration
{
    public required string SubscriptionCreatedTopic { get; init; }
    public required string VpnConfigGeneratedTopic { get; init; }
}