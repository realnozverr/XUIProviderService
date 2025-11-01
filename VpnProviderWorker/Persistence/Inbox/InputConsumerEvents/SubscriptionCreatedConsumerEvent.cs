using FluentResults;
using MediatR;
using VpnProviderWorker.Command.AddClientToInboundCommand;

namespace VpnProviderWorker.Persistence.Inbox.InputConsumerEvents;

public class SubscriptionCreatedConsumerEvent(
    Guid eventId,
    Guid subscriptionId,
    Guid userId,
    int planId,
    string telegramName,
    long telegramId,
    DateTime endDate) : IConvertibleToCommand
{
    public Guid EventId { get; }  = eventId;
    public Guid SubscriptionId { get; } = subscriptionId;
    public Guid UserId { get; } = userId;
    public string TelegramName { get; } = telegramName;
    public long TelegramId { get; } = telegramId;
    public int PlanId { get; } = planId;
    public DateTime EndDate { get; } = endDate;
    public IRequest<Result> ToCommand()
    {
       return new AddClientToInboundCommand(UserId, SubscriptionId, TelegramName,  TelegramId, EndDate, PlanId); 
    }
}