using FluentResults;
using MediatR;

namespace VpnProviderWorker.Command.AddClientToInboundCommand;

public record AddClientToInboundCommand(
    Guid UserId,
    Guid SubscriptionId,
    string TelegramName,
    long TelegramId,
    DateTime EndDate,
    int PlanId
    ) : IRequest<Result>;