
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentResults;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VpnProviderWorker.Services.Xui;

namespace VpnProviderWorker.Services;

public class XUiClient : IXUiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<XUiService> _logger;
    private readonly XUiOptions _options;

    public XUiClient(HttpClient httpClient, ILogger<XUiService> logger, IOptions<XUiOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    private class LoginResponse
    {
        public bool Success { get; set; }
        public string Msg { get; set; }
    }

    public async Task<Result> Login()
    {
        var response = await _httpClient.PostAsync("login/",
            new StringContent(JsonConvert.SerializeObject(new { username = _options.Username, password = _options.Password }),
                Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseString);

            if (loginResponse is { Success: true })
            {
                return Result.Ok();
            }
            return Result.Fail($"Login failed: {loginResponse?.Msg}");
        }
        return Result.Fail($"Failed to log in to X-UI. Status code: {response.StatusCode}");
    }
    
    public async Task<Result<ApiResponse<object>>> AddClient(int inboundId, AddClientRequest request)
    {
        var settings = new { clients = new[] { request } };
        var content = new StringContent(
            JsonConvert.SerializeObject(new { id = inboundId, settings = JsonConvert.SerializeObject(settings) }),
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("panel/api/inbounds/addClient", content);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<object>>(responseString);
            if (result is { Success: true })
            {
                return Result.Ok(result);
            }
        }
        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Failed to add client to X-UI. Status: {StatusCode}, Response: {Error}", response.StatusCode, error);
        return Result.Fail($"Failed to add client. Status: {response.StatusCode}");
    }
}
