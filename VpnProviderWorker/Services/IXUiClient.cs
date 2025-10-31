using FluentResults;
using VpnProviderWorker.Services.Xui;

namespace VpnProviderWorker.Services;

public interface IXUiClient
{
    Task<Result> Login();
    Task<Result<ApiResponse<object>>> AddClient(int inboundId, AddClientRequest request);
}