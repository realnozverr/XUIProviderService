using VpnProviderWorker.Persistence.Inbox.InputConsumerEvents;

namespace VpnProviderWorker.Persistence.Inbox;

public interface IInbox
{
    Task<bool> Save(IConvertibleToCommand  consumerEvent);
}