
using Newtonsoft.Json;

namespace VpnProviderWorker.Services.Xui;

public class ApiResponse<T>
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    [JsonProperty("msg")]
    public string Message { get; set; }
    [JsonProperty("obj")]
    public T? Obj { get; set; }
}
