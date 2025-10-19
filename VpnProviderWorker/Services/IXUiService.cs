
using FluentResults;

namespace VpnProviderWorker.Services;

public interface IXUiService
{
    Task<Result<string>> GenerateConfig(Guid userId, string email);
}
