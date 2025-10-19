
using Newtonsoft.Json;

namespace VpnProviderWorker.Services.Xui;

public class AddClientResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    [JsonProperty("msg")]
    public string Message { get; set; }
    [JsonProperty("obj")]
    public ClientInfo? Obj { get; set; }
}

public class ClientInfo
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("up")]
    public long Up { get; set; }
    [JsonProperty("down")]
    public long Down { get; set; }
    [JsonProperty("total")]
    public long Total { get; set; }
    [JsonProperty("remark")]
    public string Remark { get; set; }
    [JsonProperty("enable")]
    public bool Enable { get; set; }
    [JsonProperty("expiryTime")]
    public long ExpiryTime { get; set; }
    [JsonProperty("listen")]
    public string Listen { get; set; }
    [JsonProperty("port")]
    public int Port { get; set; }
    [JsonProperty("protocol")]
    public string Protocol { get; set; }
    [JsonProperty("settings")]
    public string Settings { get; set; }
    [JsonProperty("streamSettings")]
    public string StreamSettings { get; set; }
    [JsonProperty("tag")]
    public string Tag { get; set; }
    [JsonProperty("sniffing")]
    public string Sniffing { get; set; }
}
