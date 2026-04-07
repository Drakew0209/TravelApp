using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class TourRouteCatalogService : ITourRouteCatalogService
{
    private readonly IPoiApiClient _poiApiClient;

    public TourRouteCatalogService(IPoiApiClient poiApiClient)
    {
        _poiApiClient = poiApiClient;
    }

    public async Task<TourRouteDto?> GetRouteAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var route = ResolveRoute(poiId);
        if (route is null)
        {
            return null;
        }

        var pois = new List<PoiDto>();
        foreach (var id in route.PoiIds)
        {
            var poi = await _poiApiClient.GetByIdAsync(id, languageCode, cancellationToken);
            if (poi is null)
            {
                return null;
            }

            pois.Add(poi);
        }

        var waypoints = pois.Select((poi, index) => new TourRouteWaypointDto
        {
            SortOrder = index + 1,
            DistanceFromPreviousMeters = route.DistanceFromPreviousMeters[index],
            Poi = MapPoi(poi)
        }).ToList();

        return new TourRouteDto
        {
            Id = route.TourId,
            AnchorPoiId = route.AnchorPoiId,
            Name = route.Name,
            Description = route.Description,
            CoverImageUrl = route.CoverImageUrl,
            PrimaryLanguage = languageCode ?? "vi",
            TotalDistanceMeters = waypoints.Sum(x => x.DistanceFromPreviousMeters ?? 0),
            Waypoints = waypoints
        };
    }

    private static RouteDefinition? ResolveRoute(int poiId)
    {
        return poiId switch
        {
            1 or 2 or 3 => new RouteDefinition(
                TourId: 1,
                AnchorPoiId: 1,
                Name: "HCM Food Tour",
                Description: "Tour ẩm thực Sài Gòn với các điểm dừng được sắp xếp theo lộ trình thật.",
                CoverImageUrl: "https://images.unsplash.com/photo-1555521760-cb7ebb6a9c62?w=1200&h=800&fit=crop",
                PoiIds: new[] { 1, 2, 3 },
                DistanceFromPreviousMeters: new[] { 0d, 900d, 1100d }),
            4 or 5 or 6 => new RouteDefinition(
                TourId: 2,
                AnchorPoiId: 4,
                Name: "Hanoi Food Tour",
                Description: "Tour ẩm thực Hà Nội với các mốc waypoint, bản đồ và audio tự động.",
                CoverImageUrl: "https://images.unsplash.com/photo-1511632765486-a01980e01a18?w=1200&h=800&fit=crop",
                PoiIds: new[] { 4, 5, 6 },
                DistanceFromPreviousMeters: new[] { 0d, 300d, 500d }),
            _ => null
        };
    }

    private static PoiMobileDto MapPoi(PoiDto poi)
    {
        return new PoiMobileDto
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            Description = poi.Description,
            LanguageCode = poi.PrimaryLanguage ?? "en",
            PrimaryLanguage = poi.PrimaryLanguage ?? "en",
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters ?? 100,
            Category = poi.Category ?? string.Empty,
            AudioAssets = poi.AudioAssets.Select(x => new PoiAudioMobileDto
            {
                LanguageCode = x.LanguageCode,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                IsGenerated = x.IsGenerated
            }).ToList()
        };
    }

    private sealed record RouteDefinition(int TourId, int AnchorPoiId, string Name, string Description, string? CoverImageUrl, IReadOnlyList<int> PoiIds, IReadOnlyList<double> DistanceFromPreviousMeters);
}
