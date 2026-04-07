using System.Net.Http.Json;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Api;

public class TourApiClient : ApiClientBase, ITourApiClient
{
    public TourApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
    }

    public async Task<TourRouteDto?> GetByAnchorPoiIdAsync(int anchorPoiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? $"api/tours/{anchorPoiId}"
            : $"api/tours/{anchorPoiId}?lang={Uri.EscapeDataString(languageCode)}";

        var response = await client.GetAsync(endpoint, cancellationToken);
        return await ReadAsAsync<TourRouteDto>(response, cancellationToken);
    }
}
