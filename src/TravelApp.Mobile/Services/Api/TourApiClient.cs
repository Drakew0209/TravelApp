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
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? $"api/tours/{anchorPoiId}"
            : $"api/tours/{anchorPoiId}?lang={Uri.EscapeDataString(languageCode)}";

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken: cancellationToken);
        return NormalizeRoute(await ReadAsAsync<TourRouteDto>(response, cancellationToken));
    }

    public async Task<IReadOnlyList<TourRouteDto>> GetAllAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? "api/tours"
            : $"api/tours?lang={Uri.EscapeDataString(languageCode)}";

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken: cancellationToken);
        return NormalizeRoutes(await ReadAsAsync<List<TourRouteDto>>(response, cancellationToken) ?? []);
    }

    private TourRouteDto? NormalizeRoute(TourRouteDto? route)
    {
        if (route is null)
        {
            return null;
        }

        route.CoverImageUrl = NormalizeResourceUrl(route.CoverImageUrl);
        foreach (var waypoint in route.Waypoints)
        {
            waypoint.Poi.ImageUrl = NormalizeResourceUrl(waypoint.Poi.ImageUrl);
        }

        return route;
    }

    private IReadOnlyList<TourRouteDto> NormalizeRoutes(IReadOnlyList<TourRouteDto> routes)
    {
        foreach (var route in routes)
        {
            NormalizeRoute(route);
        }

        return routes;
    }
}
