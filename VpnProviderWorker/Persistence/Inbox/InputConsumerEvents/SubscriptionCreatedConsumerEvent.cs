using FluentResults;
using MediatR;
using VpnProviderWorker.Command.AddClientToInboundCommand;

namespace VpnProviderWorker.Persistence.Inbox.InputConsumerEvents;

public class SubscriptionCreatedConsumerEvent(
    Guid eventId,
    Guid userId,
    int planId,
    string telegramName,
    DateTime endDate) : IConvertibleToCommand
{
    public Guid EventId { get; }  = eventId;
    public Guid UserId { get; } = userId;
    public string TelegramName { get; } = telegramName;
    public int PlanId { get; } = planId;
    public IRequest<Result> ToCommand()
    {
       return new AddClientToInboundCommand(UserId, TelegramName); 
    }
}