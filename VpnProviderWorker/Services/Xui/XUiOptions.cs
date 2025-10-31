
namespace VpnProviderWorker.Services.Xui;

public class XUiOptions
{
    public const string SectionName = "XUi";
    
    public string ApiUrl { get; init; } = null!;
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
    public int InboundId { get; init; }
    public string SubscriptionBaseUrl { get; init; } = null!;
}
