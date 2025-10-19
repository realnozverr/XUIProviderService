
using Newtonsoft.Json;

namespace VpnProviderWorker.Services.Xui;

public class AddClientRequest
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("enable")]
    public bool Enable { get; set; }
    [JsonProperty("email")]
    public string Email { get; set; }
    [JsonProperty("totalGB")]
    public int TotalGb { get; set; }
    [JsonProperty("expiryTime")]
    public long ExpiryTime { get; set; }
    [JsonProperty("limitIp")]
    public int LimitIp { get; set; }
    [JsonProperty("flow")]
    public string Flow { get; set; }
    [JsonProperty("subId")]
    public string SubId { get; set; }
}
