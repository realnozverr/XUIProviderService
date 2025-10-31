using SubscriptionKafkaContracts.From.VpnServiceEvents;

namespace VpnProviderWorker.Kafka;

public interface IMessageBus
{
    Task Publish(VpnConfigGenerated message, CancellationToken token = default);
}