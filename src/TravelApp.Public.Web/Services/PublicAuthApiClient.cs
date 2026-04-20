using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelApp.Application.Abstractions.Auth;
using TravelApp.Application.Dtos.Auth;

namespace TravelApp.Public.Web.Services;

public sealed class PublicAuthApiClient : IPublicAuthApiClient
{
    private readonly HttpClient _httpClient;

    public PublicAuthApiClient(HttpClient httpClient, IOptions<TravelAppApiOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
    }

    public async Task<AuthResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequestDto(email, password), options: JsonOptions)
        }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadMessageAsync(response, cancellationToken) ?? "Unable to sign in.");
        }

        return await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions, cancellationToken);
    }

    public async Task<AuthResultDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
        {
            Content = JsonContent.Create(new RefreshTokenRequestDto(refreshToken), options: JsonOptions)
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions, cancellationToken);
    }

    public async Task<AuthResultDto?> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "api/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequestDto(email, password, fullName), options: JsonOptions)
        }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadMessageAsync(response, cancellationToken) ?? "Unable to create your account.");
        }

        return await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions, cancellationToken);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        try
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")
            {
                Content = JsonContent.Create(new { refreshToken }, options: JsonOptions)
            }, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
        }
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOptions, cancellationToken);
            if (payload is not null && payload.TryGetValue("message", out var message) && !string.IsNullOrWhiteSpace(message))
            {
                return message;
            }
        }
        catch
        {
        }

        return response.ReasonPhrase;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record LoginRequestDto(string Email, string Password);
    private sealed record RefreshTokenRequestDto(string RefreshToken);
}
