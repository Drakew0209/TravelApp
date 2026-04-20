using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using TravelApp.Application.Abstractions.Auth;
using TravelApp.Application.Dtos.Library;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Public.Web.Pages.Auth;

namespace TravelApp.Public.Web.Services;

public sealed class PublicLibraryApiProxyService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<TravelAppApiOptions> _apiOptions;
    private readonly IPublicAuthApiClient _authApiClient;
    private readonly ITravelAppPublicApiClient _publicApiClient;

    public PublicLibraryApiProxyService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IOptions<TravelAppApiOptions> apiOptions,
        IPublicAuthApiClient authApiClient,
        ITravelAppPublicApiClient publicApiClient)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _apiOptions = apiOptions;
        _authApiClient = authApiClient;
        _publicApiClient = publicApiClient;
    }

    public async Task<IReadOnlyList<PublicBookmarkItemDto>?> GetBookmarksAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        var remote = await GetRemoteBookmarksAsync(cancellationToken);
        if (remote is null)
        {
            return null;
        }

        var items = new List<PublicBookmarkItemDto>();
        foreach (var bookmark in remote)
        {
            var poi = await _publicApiClient.GetPoiAsync(bookmark.PoiId, languageCode, cancellationToken);
            if (poi is null)
            {
                continue;
            }

            items.Add(new PublicBookmarkItemDto(
                bookmark.PoiId,
                poi.Title,
                poi.Subtitle,
                poi.Location,
                poi.ImageUrl,
                string.IsNullOrWhiteSpace(languageCode) ? poi.PrimaryLanguage : languageCode,
                bookmark.SavedAtUtc,
                $"/?poiId={bookmark.PoiId}&lang={Uri.EscapeDataString(string.IsNullOrWhiteSpace(languageCode) ? poi.PrimaryLanguage : languageCode!)}"));
        }

        return items;
    }

    public async Task<IReadOnlyList<PublicHistoryItemDto>?> GetHistoryAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        var remote = await GetRemoteHistoryAsync(cancellationToken);
        if (remote is null)
        {
            return null;
        }

        var bookmarks = (await GetRemoteBookmarksAsync(cancellationToken) ?? []).Select(x => x.PoiId).ToHashSet();
        var items = new List<PublicHistoryItemDto>();
        foreach (var history in remote)
        {
            var poi = await _publicApiClient.GetPoiAsync(history.PoiId, languageCode, cancellationToken);
            if (poi is null)
            {
                continue;
            }

            var selectedLanguage = string.IsNullOrWhiteSpace(languageCode) ? poi.PrimaryLanguage : languageCode!;
            items.Add(new PublicHistoryItemDto(
                history.PoiId,
                poi.Title,
                poi.Subtitle,
                poi.Location,
                poi.ImageUrl,
                selectedLanguage,
                history.VisitedAtUtc,
                bookmarks.Contains(history.PoiId),
                $"/?poiId={history.PoiId}&lang={Uri.EscapeDataString(selectedLanguage)}"));
        }

        return items;
    }

    public Task<bool> ToggleBookmarkAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Post, $"api/library/bookmarks/{poiId}", cancellationToken);
    }

    public Task<bool> RemoveBookmarkAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Delete, $"api/library/bookmarks/{poiId}", cancellationToken);
    }

    public Task<bool> ClearBookmarksAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Delete, "api/library/bookmarks", cancellationToken);
    }

    public Task<bool> AddHistoryAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Post, $"api/library/history/{poiId}", cancellationToken);
    }

    public Task<bool> RemoveHistoryAsync(int poiId, CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Delete, $"api/library/history/{poiId}", cancellationToken);
    }

    public Task<bool> ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(HttpMethod.Delete, "api/library/history", cancellationToken);
    }

    private async Task<List<BookmarkStateDto>?> GetRemoteBookmarksAsync(CancellationToken cancellationToken)
    {
        var client = await CreateAuthorizedClientAsync(cancellationToken);
        if (client is null)
        {
            return null;
        }

        try
        {
            return await client.GetFromJsonAsync<List<BookmarkStateDto>>("api/library/bookmarks", cancellationToken) ?? [];
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<HistoryStateDto>?> GetRemoteHistoryAsync(CancellationToken cancellationToken)
    {
        var client = await CreateAuthorizedClientAsync(cancellationToken);
        if (client is null)
        {
            return null;
        }

        try
        {
            return await client.GetFromJsonAsync<List<HistoryStateDto>>("api/library/history", cancellationToken) ?? [];
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> SendAsync(HttpMethod method, string requestUri, CancellationToken cancellationToken)
    {
        var client = await CreateAuthorizedClientAsync(cancellationToken);
        if (client is null)
        {
            return false;
        }

        try
        {
            var response = await client.SendAsync(new HttpRequestMessage(method, requestUri), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<HttpClient?> CreateAuthorizedClientAsync(CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        var auth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!auth.Succeeded)
        {
            return null;
        }

        var accessToken = AuthSessionHelper.GetAccessToken(auth.Properties);
        var refreshToken = AuthSessionHelper.GetRefreshToken(auth.Properties);
        var expiresAtUtc = AuthSessionHelper.GetExpiresAtUtc(auth.Properties);

        if ((string.IsNullOrWhiteSpace(accessToken) || (expiresAtUtc.HasValue && expiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(1)))
            && !string.IsNullOrWhiteSpace(refreshToken))
        {
            var refreshed = await _authApiClient.RefreshAsync(refreshToken, cancellationToken);
            if (refreshed is not null)
            {
                var email = auth.Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
                var fullName = refreshed.FullName ?? auth.Principal?.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
                await AuthSessionHelper.SignInAsync(context, refreshed, email, fullName);
                accessToken = refreshed.AccessToken;
            }
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_apiOptions.Value.BaseUrl, UriKind.Absolute);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(auth.Properties?.GetTokenValue("token_type") ?? "Bearer", accessToken);
        return client;
    }
}

public sealed record PublicBookmarkItemDto(int PoiId, string Title, string? Subtitle, string? Location, string? ImageUrl, string? LanguageCode, DateTimeOffset SavedAtUtc, string Link);
public sealed record PublicHistoryItemDto(int PoiId, string Title, string? Subtitle, string? Location, string? ImageUrl, string? LanguageCode, DateTimeOffset VisitedAtUtc, bool IsBookmarked, string Link);
