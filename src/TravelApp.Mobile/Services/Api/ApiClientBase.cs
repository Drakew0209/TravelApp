using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public abstract class ApiClientBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiClientOptions _options;
    private readonly ITokenStore _tokenStore;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    protected ApiClientBase(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _tokenStore = tokenStore;
    }

    protected static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected HttpClient CreateClient(bool authorized = false)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl);

        if (authorized && !string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(_tokenStore.TokenType, _tokenStore.AccessToken);
        }

        return client;
    }

    protected async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory, bool authorized = false, CancellationToken cancellationToken = default)
    {
        var response = await SendOnceAsync(requestFactory, authorized, cancellationToken);
        if (!authorized || response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return response;
        }

        if (!await TryRefreshAccessTokenAsync(cancellationToken))
        {
            return response;
        }

        response.Dispose();
        return await SendOnceAsync(requestFactory, authorized: true, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> requestFactory, bool authorized, CancellationToken cancellationToken)
    {
        var client = CreateClient(authorized);
        var request = requestFactory();
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<bool> TryRefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        var refreshToken = _tokenStore.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            if (!string.Equals(_tokenStore.RefreshToken, refreshToken, StringComparison.Ordinal))
            {
                return !string.IsNullOrWhiteSpace(_tokenStore.AccessToken);
            }

            var client = CreateClient();
            var response = await client.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequestDto(refreshToken), JsonOptions, cancellationToken);
            var result = await ReadAsAsync<AuthResultDto>(response, cancellationToken);
            if (result is null)
            {
                ClearTokenStore();
                AuthStateService.IsLoggedIn = false;
                return false;
            }

            _tokenStore.AccessToken = result.AccessToken;
            _tokenStore.RefreshToken = result.RefreshToken;
            _tokenStore.ExpiresAtUtc = result.ExpiresAtUtc;
            _tokenStore.TokenType = string.IsNullOrWhiteSpace(result.TokenType) ? "Bearer" : result.TokenType;
            return true;
        }
        catch
        {
            ClearTokenStore();
            AuthStateService.IsLoggedIn = false;
            return false;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private void ClearTokenStore()
    {
        _tokenStore.AccessToken = null;
        _tokenStore.RefreshToken = null;
        _tokenStore.ExpiresAtUtc = null;
        _tokenStore.TokenType = "Bearer";
    }

    protected static async Task<T?> ReadAsAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            return default;

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }
}
