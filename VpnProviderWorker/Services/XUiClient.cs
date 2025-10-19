
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentResults;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VpnProviderWorker.Services.Xui;

namespace VpnProviderWorker.Services;

public class XUiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<XUiService> _logger;
    private readonly XUiOptions _options;
    private CookieContainer _cookieContainer = new();

    public XUiClient(HttpClient httpClient, ILogger<XUiService> logger, IOptions<XUiOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            // WARNING: This bypasses SSL certificate validation.
            // Only use this in development or if you absolutely trust the server.
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true 
        };
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(_options.ApiUrl) };
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
                _logger.LogInformation("Successfully logged in to X-UI");

                var cookies = _cookieContainer.GetCookies(_httpClient.BaseAddress!);
                if (cookies.Count > 0)
                {
                    foreach (Cookie cookie in cookies)
                    {
                        _logger.LogInformation("Cookie found: Name={Name}, Value={Value}", cookie.Name, cookie.Value);
                    }
                }
                else
                {
                    _logger.LogWarning("Login reported success, but no cookies were found in the container.");
                }

                return Result.Ok();
            }
            
            _logger.LogError("Login failed according to response body. Message: {Message}", loginResponse?.Msg);
            return Result.Fail($"Login failed: {loginResponse?.Msg}");
        }

        _logger.LogError("Failed to log in to X-UI. Status code: {StatusCode}", response.StatusCode);
        return Result.Fail($"Failed to log in to X-UI. Status code: {response.StatusCode}");
    }
    
    public async Task<Result<AddClientResponse>> AddClient(int inboundId, AddClientRequest request)
    {
        var settings = new { clients = new[] { request } };
        var content = new StringContent(
            JsonConvert.SerializeObject(new { id = inboundId, settings = JsonConvert.SerializeObject(settings) }),
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("panel/api/inbounds/addClient", content);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AddClientResponse>(responseString);
            if (result is { Success: true })
            {
                return Result.Ok(result);
            }
        }
        
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("X-UI returned an error. Attempting to log in again.");
            var loginResult = await Login();
            if (loginResult.IsSuccess)
            {
                response = await _httpClient.PostAsync("panel/api/inbounds/addClient", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AddClientResponse>(responseString);
                    if (result is { Success: true })
                    {
                        return Result.Ok(result);
                    }
                }
            }
        }

        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Failed to add client to X-UI. Status: {StatusCode}, Response: {Error}", response.StatusCode, error);
        return Result.Fail($"Failed to add client. Status: {response.StatusCode}");
    }
}
