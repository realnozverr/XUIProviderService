using FluentResults;
using MediatR;

namespace VpnProviderWorker.Command.AddClientToInboundCommand;

public record AddClientToInboundCommand(Guid UserId, string TelegramName) : IRequest<Result>;