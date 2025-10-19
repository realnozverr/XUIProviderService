using FluentResults;
using MediatR;

namespace VpnProviderWorker.Persistence.Inbox.InputConsumerEvents;

public interface IConvertibleToCommand
{
    public Guid EventId { get; }
    IRequest<Result> ToCommand();
}