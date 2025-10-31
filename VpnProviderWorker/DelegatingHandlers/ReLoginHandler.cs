using System.Net;
using VpnProviderWorker.Services;

namespace VpnProviderWorker.DelegatingHandlers;

public class ReLoginHandler(
    IServiceProvider serviceProvider,
    ILogger<ReLoginHandler> logger)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            logger.LogWarning("Request failed with {StatusCode}. Attempting to re-login", response.StatusCode);

            await using var scope = serviceProvider.CreateAsyncScope();
            var xuiClient = scope.ServiceProvider.GetRequiredService<IXUiClient>();

            var loginResult = await xuiClient.Login();

            if (loginResult.IsSuccess)
            {
                logger.LogInformation("Re-login successful. Retrying the original request to {RequestUri}", request.RequestUri);
                
                response = await base.SendAsync(request, cancellationToken);
            }
            else
            {
                logger.LogError("Failed to re-login. Cannot retry the original request.");
            }
        }
        return response;
    }
}