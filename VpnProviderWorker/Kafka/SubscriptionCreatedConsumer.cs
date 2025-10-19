using MassTransit;
using SubscriptionKafkaContracts.From.SubscriptionKafkaEvents;
using VpnProviderWorker.Command.AddClientToInboundCommand;
using VpnProviderWorker.Persistence.Inbox;
using VpnProviderWorker.Persistence.Inbox.InputConsumerEvents;

namespace VpnProviderWorker.Kafka;

public class SubscriptionCreatedConsumer(IInbox  inbox) : IConsumer<SubscriptionCreated>
{
    public async Task Consume(ConsumeContext<SubscriptionCreated> context)
    {
        var @event = context.Message;
        var subscriptionCreatedConsumerEvent = new SubscriptionCreatedConsumerEvent(
            @event.EventId,
            @event.UserId,
            @event.PlanId,
            @event.TelegramName,
            @event.EndDate);

        var isSaved = await inbox.Save(subscriptionCreatedConsumerEvent);
        if (isSaved == false)
            throw new Exception("Failed to save inbox event");

        await context.ConsumeCompleted;
    }
}