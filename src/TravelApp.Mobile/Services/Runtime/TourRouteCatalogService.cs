using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class TourRouteCatalogService : ITourRouteCatalogService
{
    private readonly ITourApiClient _tourApiClient;
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly ITourRouteCacheService _tourRouteCacheService;

    public TourRouteCatalogService(
        ITourApiClient tourApiClient,
        IPoiApiClient poiApiClient,
        ILocalDatabaseService localDatabaseService,
        ITourRouteCacheService tourRouteCacheService)
    {
        _tourApiClient = tourApiClient;
        _poiApiClient = poiApiClient;
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
        return cachedRoutes.Count > 0 ? cachedRoutes : [];
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
        PoiDto? poiDetails = null;

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            try
            {
                poiDetails = await _poiApiClient.GetByIdAsync(poiId, normalizedLanguage, cancellationToken);
            }
            catch
            {
            }
        }

        var routes = await GetAllRoutesAsync(normalizedLanguage, cancellationToken);
        var routePoi = routes
            .SelectMany(x => x.Waypoints)
            .Select(x => x.Poi)
            .FirstOrDefault(x => x.Id == poiId);

        if (routePoi is not null)
        {
            var dto = new PoiDto
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

            if (poiDetails is not null)
            {
                MergeSpeechData(dto, poiDetails);
            }

            return dto;
        }

        if (poiDetails is not null)
        {
            return poiDetails;
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

    private static void MergeSpeechData(PoiDto target, PoiDto source)
    {
        if (!string.IsNullOrWhiteSpace(source.SpeechText))
        {
            target.SpeechText = source.SpeechText;
        }

        if (!string.IsNullOrWhiteSpace(source.SpeechTextLanguageCode))
        {
            target.SpeechTextLanguageCode = source.SpeechTextLanguageCode;
        }

        if (source.SpeechTexts.Count > 0)
        {
            target.SpeechTexts = source.SpeechTexts;
        }
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

            foreach (var route in routes)
            {
                route.CoverImageUrl = NormalizeCoverImageUrl(route.CoverImageUrl, route.Name);
            }

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
