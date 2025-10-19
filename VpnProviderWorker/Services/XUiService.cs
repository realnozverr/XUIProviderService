using FluentResults;
using Microsoft.Extensions.Options;
using VpnProviderWorker.Services.Xui;

namespace VpnProviderWorker.Services;

public class XUiService : IXUiService
{
    private readonly XUiClient _client;
    private readonly ILogger<XUiService> _logger;
    private readonly XUiOptions _options;

    public XUiService(XUiClient client, ILogger<XUiService> logger, IOptions<XUiOptions> options)
    {
        _client = client;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Result<string>> GenerateConfig(Guid userId, string email)
    {
        _logger.LogInformation("Generating config for user {UserId}", userId);

        var request = new AddClientRequest
        {
            Id = userId.ToString(),
            Email = email,
            Enable = true,
            TotalGb = 0, 
            ExpiryTime = 0, 
            LimitIp = 0,
            Flow = "xtls-rprx-vision",
            SubId = Guid.NewGuid().ToString("N")
        };

        var result = await _client.AddClient(_options.InboundId, request);

        if (result.IsFailed)
        {
            return Result.Fail("Failed to add client in X-UI").WithErrors(result.Errors);
        }
        
        var clientInfo = result.Value.Obj;
        if (clientInfo == null)
        {
            _logger.LogWarning("AddClient call to X-UI succeeded, but the response object was null. Cannot generate a full config string.");
            // Since the client was added successfully, we can return a success result but with a placeholder or partial config.
            // For now, we return a success to acknowledge the client was created.
            return Result.Ok($"Client {userId} created successfully, but config string could not be generated.");
        }

        var config = $"{clientInfo.Protocol}://{userId}@{clientInfo.Listen}:{clientInfo.Port}?type=tcp&flow=xtls-rprx-vision#sub_{userId}";

        _logger.LogInformation("Generated config for user {UserId}: {Config}", userId, config);
        return Result.Ok(config);
    }
}

