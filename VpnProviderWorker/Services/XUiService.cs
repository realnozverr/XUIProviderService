using FluentResults;
using Microsoft.Extensions.Options;
using VpnProviderWorker.Services.Xui;

namespace VpnProviderWorker.Services;

public class XUiService : IXUiService
{
    private readonly IXUiClient _client;
    private readonly ILogger<XUiService> _logger;
    private readonly XUiOptions _options;

    public XUiService(IXUiClient client, ILogger<XUiService> logger, IOptions<XUiOptions> options)
    {
        _client = client;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Result<string>> GenerateConfig(
        Guid userId,
        string email,
        long expiryTime,
        Guid subId,
        int totalGb,
        int limitIp
        )
    {
        _logger.LogInformation("Generating config for user {UserId}", userId);
        var request = new AddClientRequest
        {
            Id = userId.ToString(),
            Email = email,
            Enable = true,
            TotalGb = totalGb, 
            ExpiryTime = expiryTime,
            LimitIp = limitIp,
            Flow = "xtls-rprx-vision",
            SubId = subId.ToString()
        };

        var result = await _client.AddClient(_options.InboundId, request);

        if (result.IsFailed)
        {
            return Result.Fail("Failed to add client in X-UI").WithErrors(result.Errors);
        }

        var subscriptionLink = $"{_options.SubscriptionBaseUrl}{request.SubId}";

        _logger.LogInformation("Generated config for user {UserId}: {subId}", userId, request.SubId);
        return Result.Ok(subscriptionLink);
    }
}

