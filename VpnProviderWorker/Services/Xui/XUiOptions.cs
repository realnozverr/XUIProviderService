
namespace VpnProviderWorker.Services.Xui;

public class XUiOptions
{
    public const string SectionName = "XUi";
    
    public string ApiUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int InboundId { get; set; }
}
