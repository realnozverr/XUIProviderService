using MassTransit;
using Microsoft.Extensions.Options;
using SubscriptionKafkaContracts.From.VpnServiceEvents;

namespace VpnProviderWorker.Kafka;

public class KafkaProducer(
    ITopicProducerProvider topicProducerProvider,
    IOptions<KafkaTopicsConfiguration> topicsConfiguration) : IMessageBus
{
    private readonly KafkaTopicsConfiguration _topicsConfiguration = topicsConfiguration.Value;
    public async Task Publish(VpnConfigGenerated message, CancellationToken cancellationtoken = default)
    {
        var producer = topicProducerProvider.GetProducer<string, VpnConfigGenerated>(
            new Uri($"topic:{_topicsConfiguration.VpnConfigGeneratedTopic}"));

        await producer.Produce(message.EventId.ToString(), message, cancellationtoken);
    }
}