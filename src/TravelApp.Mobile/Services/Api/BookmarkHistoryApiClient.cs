using System.Net.Http.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public sealed class BookmarkHistoryApiClient : ApiClientBase, IBookmarkHistoryApiClient
{
    public BookmarkHistoryApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
    }

    public async Task<IReadOnlyList<BookmarkStateDto>?> GetBookmarksAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "api/library/bookmarks"), authorized: true, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadAsAsync<List<BookmarkStateDto>>(response, cancellationToken) ?? [];
    }

    public async Task<bool> ToggleBookmarkAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"api/library/bookmarks/{poiId}"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveBookmarkAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/library/bookmarks/{poiId}"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ClearBookmarksAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, "api/library/bookmarks"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<HistoryStateDto>?> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "api/library/history"), authorized: true, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadAsAsync<List<HistoryStateDto>>(response, cancellationToken) ?? [];
    }

    public async Task<bool> AddHistoryAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"api/library/history/{poiId}"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveHistoryAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/library/history/{poiId}"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, "api/library/history"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
