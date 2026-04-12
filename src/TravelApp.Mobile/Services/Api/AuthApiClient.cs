using System.Net.Http.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public class AuthApiClient : ApiClientBase, IAuthApiClient
{
    private readonly ITokenStore _tokenStore;

    public AuthApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public async Task<AuthResultDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, cancellationToken: cancellationToken);
        var result = await ReadAsAsync<AuthResultDto>(response, cancellationToken);

        PersistToken(result);

        return result;
    }

    public async Task<AuthResultDto?> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/auth/register")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, cancellationToken: cancellationToken);
        var result = await ReadAsAsync<AuthResultDto>(response, cancellationToken);

        PersistToken(result);

        return result;
    }

    public async Task<AuthResultDto?> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, cancellationToken: cancellationToken);
        var result = await ReadAsAsync<AuthResultDto>(response, cancellationToken);

        PersistToken(result);

        return result;
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return LogoutAsyncInternal(cancellationToken);
    }

    private async Task LogoutAsyncInternal(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_tokenStore.RefreshToken))
            {
                return;
            }

            var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")
            {
                Content = JsonContent.Create(new { refreshToken = _tokenStore.RefreshToken }, options: JsonOptions)
            }, cancellationToken: cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
        }
        finally
        {
            _tokenStore.AccessToken = null;
            _tokenStore.RefreshToken = null;
            _tokenStore.ExpiresAtUtc = null;
            _tokenStore.TokenType = "Bearer";
        }
    }

    private void PersistToken(AuthResultDto? result)
    {
        if (result is null)
            return;

        _tokenStore.AccessToken = result.AccessToken;
        _tokenStore.RefreshToken = result.RefreshToken;
        _tokenStore.ExpiresAtUtc = result.ExpiresAtUtc;
        _tokenStore.TokenType = string.IsNullOrWhiteSpace(result.TokenType) ? "Bearer" : result.TokenType;
    }
}
