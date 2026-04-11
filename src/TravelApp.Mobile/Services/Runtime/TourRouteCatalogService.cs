using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class TourRouteCatalogService : ITourRouteCatalogService
{
    private readonly ITourApiClient _tourApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly ITourRouteCacheService _tourRouteCacheService;

    public TourRouteCatalogService(
        ITourApiClient tourApiClient,
        ILocalDatabaseService localDatabaseService,
        ITourRouteCacheService tourRouteCacheService)
    {
        _tourApiClient = tourApiClient;
        _localDatabaseService = localDatabaseService;
        _tourRouteCacheService = tourRouteCacheService;
    }

    public async Task<IReadOnlyList<TourRouteDto>> GetAllRoutesAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        var onlineRoutes = await TryGetOnlineRoutesAsync(normalizedLanguage, cancellationToken);
        if (onlineRoutes.Count > 0)
        {
            return onlineRoutes;
        }

        var cachedRoutes = await _tourRouteCacheService.GetAllAsync(normalizedLanguage, cancellationToken);
        if (cachedRoutes.Count > 0)
        {
            return await MergeLocalPoiOverridesAsync(cachedRoutes.ToList(), normalizedLanguage, cancellationToken);
        }

        return [];
    }

    public async Task<TourRouteDto?> GetRouteAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        var routes = await GetAllRoutesAsync(normalizedLanguage, cancellationToken);
        var route = routes.FirstOrDefault(x => x.AnchorPoiId == poiId || x.Waypoints.Any(w => w.Poi.Id == poiId));
        return route is not null ? route : await _tourRouteCacheService.GetAsync(poiId, normalizedLanguage, cancellationToken);
    }

    public async Task<PoiDto?> ResolvePoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        var routes = await GetAllRoutesAsync(normalizedLanguage, cancellationToken);
        var routePoi = routes
            .SelectMany(x => x.Waypoints)
            .Select(x => x.Poi)
            .FirstOrDefault(x => x.Id == poiId);

        if (routePoi is not null)
        {
            return new PoiDto
            {
                Id = routePoi.Id,
                Title = routePoi.Title,
                Subtitle = routePoi.Subtitle,
                ImageUrl = routePoi.ImageUrl,
                Location = routePoi.Location,
                Latitude = routePoi.Latitude,
                Longitude = routePoi.Longitude,
                GeofenceRadiusMeters = routePoi.GeofenceRadiusMeters,
                Distance = routePoi.DistanceMeters.HasValue ? $"{routePoi.DistanceMeters.Value:F0} m" : string.Empty,
                Duration = string.Empty,
                Description = routePoi.Description,
                Provider = string.Empty,
                Credit = string.Empty,
                Category = routePoi.Category,
                PrimaryLanguage = routePoi.PrimaryLanguage,
                SpeechText = routePoi.SpeechText,
                SpeechTextLanguageCode = routePoi.SpeechTextLanguageCode,
                Localizations = [],
                AudioAssets = routePoi.AudioAssets.Select(x => new PoiAudioDto(x.LanguageCode, x.AudioUrl, x.Transcript, x.IsGenerated)).ToList(),
                SpeechTexts = routePoi.SpeechTexts.Select(x => new PoiSpeechTextDto(x.LanguageCode, x.Text)).ToList()
            };
        }

        var localPois = await _localDatabaseService.GetPoisAsync(normalizedLanguage, cancellationToken: cancellationToken);
        var localPoi = localPois.FirstOrDefault(x => x.Id == poiId);
        if (localPoi is null)
        {
            return null;
        }

        return new PoiDto
        {
            Id = localPoi.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            Description = localPoi.Description,
            PrimaryLanguage = string.IsNullOrWhiteSpace(localPoi.PrimaryLanguage) ? normalizedLanguage : localPoi.PrimaryLanguage,
            ImageUrl = localPoi.ImageUrl,
            Location = localPoi.Location,
            Latitude = localPoi.Latitude,
            Longitude = localPoi.Longitude,
            GeofenceRadiusMeters = localPoi.GeofenceRadiusMeters,
            Distance = string.Empty,
            Duration = string.Empty,
            Category = localPoi.Category,
            SpeechText = localPoi.SpeechText,
            SpeechTextLanguageCode = localPoi.SpeechTextLanguageCode,
            Localizations = [],
            AudioAssets = localPoi.AudioAssets.Select(x => new PoiAudioDto(x.LanguageCode, x.AudioUrl, x.Transcript, x.IsGenerated)).ToList(),
            SpeechTexts = localPoi.SpeechTexts.Select(x => new PoiSpeechTextDto(x.LanguageCode, x.Text)).ToList()
        };
    }

    private async Task<List<TourRouteDto>> TryGetOnlineRoutesAsync(string languageCode, CancellationToken cancellationToken)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return [];
        }

        try
        {
            var routes = (await _tourApiClient.GetAllAsync(languageCode, cancellationToken)).ToList();
            if (routes.Count == 0)
            {
                return [];
            }

            routes = await MergeLocalPoiOverridesAsync(routes, languageCode, cancellationToken);
            foreach (var route in routes)
            {
                await _tourRouteCacheService.SaveAsync(route, cancellationToken);
            }

            return routes;
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<TourRouteDto>> MergeLocalPoiOverridesAsync(List<TourRouteDto> routes, string languageCode, CancellationToken cancellationToken)
    {
        var localPois = await _localDatabaseService.GetPoisAsync(languageCode, cancellationToken: cancellationToken);
        var localById = localPois.ToDictionary(x => x.Id);

        foreach (var route in routes)
        {
            route.CoverImageUrl = NormalizeCoverImageUrl(route.CoverImageUrl, route.Name);
            route.Waypoints = route.Waypoints.Select(waypoint => MergeWaypoint(waypoint, localById)).ToList();
        }

        return routes;
    }

    private static TourRouteWaypointDto MergeWaypoint(TourRouteWaypointDto waypoint, IReadOnlyDictionary<int, PoiMobileDto> localById)
    {
        if (!localById.TryGetValue(waypoint.Poi.Id, out var localPoi))
        {
            return waypoint;
        }

        if (localPoi.UpdatedAtUtc <= waypoint.Poi.UpdatedAtUtc)
        {
            return waypoint;
        }

        waypoint.Poi = new PoiMobileDto
        {
            Id = waypoint.Poi.Id,
            Title = localPoi.Title,
            Subtitle = localPoi.Subtitle,
            Description = localPoi.Description,
            LanguageCode = localPoi.LanguageCode,
            PrimaryLanguage = string.IsNullOrWhiteSpace(localPoi.PrimaryLanguage) ? waypoint.Poi.PrimaryLanguage : localPoi.PrimaryLanguage,
            ImageUrl = localPoi.ImageUrl,
            Location = localPoi.Location,
            Latitude = localPoi.Latitude,
            Longitude = localPoi.Longitude,
            DistanceMeters = waypoint.Poi.DistanceMeters,
            GeofenceRadiusMeters = localPoi.GeofenceRadiusMeters,
            Category = localPoi.Category,
            SpeechText = localPoi.SpeechText,
            SpeechTextLanguageCode = localPoi.SpeechTextLanguageCode,
            UpdatedAtUtc = localPoi.UpdatedAtUtc,
            AudioAssets = localPoi.AudioAssets,
            SpeechTexts = localPoi.SpeechTexts,
        };

        return waypoint;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
    }

    private static string NormalizeCoverImageUrl(string? coverImageUrl, string tourName)
    {
        if (!string.IsNullOrWhiteSpace(coverImageUrl) && !coverImageUrl.Contains("unsplash.com", StringComparison.OrdinalIgnoreCase))
        {
            return coverImageUrl;
        }

        return $"https://placehold.co/1200x800/png?text={Uri.EscapeDataString(tourName)}";
    }
}
