using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Domain.Entities;
using TravelApp.Infrastructure.Persistence;

namespace TravelApp.Infrastructure.Services.Pois;

public class PoiQueryService : IPoiQueryService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const double EarthRadiusMeters = 6371000;

    private readonly TravelAppDbContext _dbContext;

    public PoiQueryService(TravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDto<PoiMobileDto>> GetAllAsync(PoiQueryRequestDto request, CancellationToken cancellationToken = default)
    {
        var languageCode = request.LanguageCode;
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

        var hasGeoFilter = HasGeoFilter(request);
        Dictionary<int, double>? distanceByPoiId = null;
        List<int> pagedPoiIds;
        int totalCount;

        if (hasGeoFilter)
        {
            var lat = request.Latitude!.Value;
            var lng = request.Longitude!.Value;
            var radiusMeters = request.RadiusMeters!.Value;

            var latDelta = radiusMeters / 111320d;
            var safeCos = Math.Max(0.01, Math.Abs(Math.Cos(ToRadians(lat))));
            var lngDelta = radiusMeters / (111320d * safeCos);

            var minLat = lat - latDelta;
            var maxLat = lat + latDelta;
            var minLng = lng - lngDelta;
            var maxLng = lng + lngDelta;

            var candidates = await _dbContext.Pois
                .AsNoTracking()
                .Where(x => x.Latitude >= minLat
                            && x.Latitude <= maxLat
                            && x.Longitude >= minLng
                            && x.Longitude <= maxLng)
                .Select(x => new
                {
                    x.Id,
                    x.Latitude,
                    x.Longitude
                })
                .ToListAsync(cancellationToken);

            var filtered = candidates
                .Select(x => new
                {
                    x.Id,
                    Distance = CalculateHaversineDistanceMeters(lat, lng, x.Latitude, x.Longitude)
                })
                .Where(x => x.Distance <= radiusMeters)
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.Id)
                .ToList();

            totalCount = filtered.Count;
            distanceByPoiId = filtered.ToDictionary(x => x.Id, x => x.Distance);

            pagedPoiIds = filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Id)
                .ToList();
        }
        else
        {
            var query = _dbContext.Pois.AsNoTracking();
            totalCount = await query.CountAsync(cancellationToken);

            pagedPoiIds = await query
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        if (pagedPoiIds.Count == 0)
        {
            return new PagedResultDto<PoiMobileDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = []
            };
        }

        var usedPoiIds = await GetUsedPoiIdsAsync(cancellationToken);

        var pois = await _dbContext.Pois
            .AsNoTracking()
            .Where(x => pagedPoiIds.Contains(x.Id))
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .ToListAsync(cancellationToken);

        var orderMap = pagedPoiIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);

        var items = pois
            .Select(x => MapToMobileDto(x, languageCode, distanceByPoiId, usedPoiIds))
            .OrderBy(x => orderMap[x.Id])
            .ToList();

        return new PagedResultDto<PoiMobileDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<PoiMobileDto?> GetByIdAsync(int id, string? languageCode, CancellationToken cancellationToken = default)
    {
        var usedPoiIds = await GetUsedPoiIdsAsync(cancellationToken);

        var poi = await _dbContext.Pois
            .AsNoTracking()
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return poi is null ? null : MapToMobileDto(poi, languageCode, null, usedPoiIds);
    }

    public async Task<PoiMobileDto> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var poi = new Poi();
        ApplyRequest(poi, request);

        _dbContext.Pois.Add(poi);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToMobileDto(poi, request.PrimaryLanguage);
    }

    public async Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois
            .Include(x => x.Localizations)
            .Include(x => x.AudioAssets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (poi is null)
        {
            return false;
        }

        ApplyRequest(poi, request);
        poi.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var poi = await _dbContext.Pois.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (poi is null)
        {
            return false;
        }

        if (await _dbContext.Tours.AnyAsync(x => x.AnchorPoiId == id, cancellationToken) ||
            await _dbContext.TourPois.AnyAsync(x => x.PoiId == id, cancellationToken))
        {
            throw new InvalidOperationException("POI is used in a tour and cannot be deleted.");
        }

        _dbContext.Pois.Remove(poi);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PoiMobileDto MapToMobileDto(Poi poi, string? requestedLanguageCode, IReadOnlyDictionary<int, double>? distanceByPoiId = null, ISet<int>? usedPoiIds = null)
    {
        var requestedLanguage = NormalizeLanguageCode(requestedLanguageCode);
        var primaryLanguage = NormalizeLanguageCode(poi.PrimaryLanguage);

        var localization = ResolveLocalization(poi, requestedLanguage, primaryLanguage);
        var effectiveLanguage = localization?.LanguageCode ?? primaryLanguage;
        var speechTexts = DeserializeSpeechTexts(poi.SpeechTextsJson);
        var preferredSpeechLanguage = NormalizeLanguageCode(poi.SpeechTextLanguageCode);
        var speechLanguage = string.IsNullOrWhiteSpace(preferredSpeechLanguage) ? requestedLanguage : preferredSpeechLanguage;
        var speech = ResolveSpeechText(speechTexts, speechLanguage, primaryLanguage, poi.SpeechText, poi.Description);

        var dto = new PoiMobileDto
        {
            Id = poi.Id,
            Title = localization?.Title ?? poi.Title,
            Subtitle = localization?.Subtitle ?? poi.Subtitle ?? string.Empty,
            Description = localization?.Description ?? poi.Description ?? string.Empty,
            LanguageCode = effectiveLanguage,
            PrimaryLanguage = primaryLanguage,
            ImageUrl = poi.ImageUrl ?? string.Empty,
            Location = poi.Location ?? string.Empty,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            DistanceMeters = distanceByPoiId is not null && distanceByPoiId.TryGetValue(poi.Id, out var distance) ? distance : null,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
            Category = poi.Category ?? string.Empty,
            SpeechText = speech.Text,
            SpeechTextLanguageCode = string.IsNullOrWhiteSpace(preferredSpeechLanguage) ? speech.LanguageCode : preferredSpeechLanguage,
            UpdatedAtUtc = poi.UpdatedAtUtc ?? DateTimeOffset.UtcNow,
            IsUsedInTour = usedPoiIds?.Contains(poi.Id) ?? false,
            Localizations = poi.Localizations
                .Select(x => new PoiLocalizationDto
                {
                    LanguageCode = x.LanguageCode,
                    Title = x.Title,
                    Subtitle = x.Subtitle,
                    Description = x.Description
                })
                .ToList(),
            AudioAssets = poi.AudioAssets
                .OrderByDescending(x => string.Equals(x.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => string.Equals(x.LanguageCode, primaryLanguage, StringComparison.OrdinalIgnoreCase))
                .Select(x => new PoiAudioMobileDto
                {
                    Id = x.Id,
                    LanguageCode = x.LanguageCode,
                    AudioUrl = x.AudioUrl,
                    Transcript = x.Transcript,
                    IsGenerated = x.IsGenerated
                })
                .ToList(),
            SpeechTexts = speechTexts
                .Select(x => new PoiSpeechTextMobileDto { LanguageCode = x.LanguageCode, Text = x.Text })
                .ToList()
        };

        return dto;
    }

    private async Task<HashSet<int>> GetUsedPoiIdsAsync(CancellationToken cancellationToken)
    {
        var anchorPoiIds = _dbContext.Tours.AsNoTracking().Select(x => x.AnchorPoiId);
        var tourPoiIds = _dbContext.TourPois.AsNoTracking().Select(x => x.PoiId);

        return await anchorPoiIds
            .Concat(tourPoiIds)
            .Distinct()
            .ToHashSetAsync(cancellationToken);
    }

    private static bool HasGeoFilter(PoiQueryRequestDto request)
    {
        return request.Latitude.HasValue
               && request.Longitude.HasValue
               && request.RadiusMeters.HasValue
               && request.RadiusMeters.Value > 0;
    }

    private static double CalculateHaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double value)
    {
        return value * Math.PI / 180d;
    }

    private static PoiLocalization? ResolveLocalization(Poi poi, string requestedLanguage, string primaryLanguage)
    {
        return poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase))
               ?? poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, primaryLanguage, StringComparison.OrdinalIgnoreCase))
               ?? poi.Localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, "en", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static void ApplyRequest(Poi poi, UpsertPoiRequestDto request)
    {
        poi.Title = request.Title;
        poi.Subtitle = request.Subtitle;
        poi.Description = request.Description;
        poi.Category = request.Category;
        poi.Location = request.Location;
        poi.ImageUrl = request.ImageUrl;
        poi.Latitude = request.Latitude;
        poi.Longitude = request.Longitude;
        poi.GeofenceRadiusMeters = request.GeofenceRadiusMeters;
        poi.PrimaryLanguage = NormalizeLanguageCode(request.PrimaryLanguage);
        var speechTexts = NormalizeSpeechTexts(request.SpeechTexts, request.SpeechText, request.SpeechTextLanguageCode, request.PrimaryLanguage);
        poi.SpeechTextsJson = JsonSerializer.Serialize(speechTexts);
        poi.SpeechText = ResolveLegacySpeechText(speechTexts, poi.PrimaryLanguage, request.Description);
        poi.SpeechTextLanguageCode = NormalizeLanguageCode(request.SpeechTextLanguageCode ?? request.PrimaryLanguage);

        if (request.Localizations.Count > 0)
        {
            poi.Localizations.Clear();
            foreach (var localization in request.Localizations)
            {
                poi.Localizations.Add(new PoiLocalization
                {
                    LanguageCode = NormalizeLanguageCode(localization.LanguageCode),
                    Title = localization.Title,
                    Subtitle = localization.Subtitle,
                    Description = localization.Description
                });
            }
        }

        poi.AudioAssets.Clear();
        foreach (var audio in request.AudioAssets)
        {
            poi.AudioAssets.Add(new PoiAudio
            {
                LanguageCode = NormalizeLanguageCode(audio.LanguageCode),
                AudioUrl = audio.AudioUrl,
                Transcript = audio.Transcript,
                IsGenerated = audio.IsGenerated,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
    }

    private static List<PoiSpeechTextMobileDto> DeserializeSpeechTexts(string? json)
    {
        try
        {
            return string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<PoiSpeechTextMobileDto>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static (string Text, string LanguageCode) ResolveSpeechText(
        IReadOnlyList<PoiSpeechTextMobileDto> speechTexts,
        string requestedLanguage,
        string primaryLanguage,
        string? legacySpeechText,
        string? fallbackDescription)
    {
        var selected = speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), requestedLanguage, StringComparison.OrdinalIgnoreCase))
                       ?? speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), "vi", StringComparison.OrdinalIgnoreCase))
                       ?? speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), primaryLanguage, StringComparison.OrdinalIgnoreCase))
                       ?? speechTexts.FirstOrDefault();

        if (selected is not null && !string.IsNullOrWhiteSpace(selected.Text))
        {
            return (selected.Text, NormalizeLanguageCode(selected.LanguageCode));
        }

        if (!string.IsNullOrWhiteSpace(legacySpeechText))
        {
            return (legacySpeechText!, primaryLanguage);
        }

        if (!string.IsNullOrWhiteSpace(fallbackDescription))
        {
            return (fallbackDescription!, primaryLanguage);
        }

        return (string.Empty, primaryLanguage);
    }

    private static List<PoiSpeechTextMobileDto> NormalizeSpeechTexts(IReadOnlyList<UpsertPoiSpeechTextDto>? speechTexts, string? legacySpeechText, string? legacySpeechLanguageCode, string? primaryLanguage)
    {
        var normalized = speechTexts?
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => new PoiSpeechTextMobileDto
            {
                LanguageCode = NormalizeLanguageCode(x.LanguageCode),
                Text = x.Text.Trim()
            })
            .GroupBy(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList() ?? [];

        if (normalized.Count > 0)
        {
            return normalized;
        }

        if (!string.IsNullOrWhiteSpace(legacySpeechText))
        {
            return [new PoiSpeechTextMobileDto
            {
                LanguageCode = NormalizeLanguageCode(legacySpeechLanguageCode ?? primaryLanguage),
                Text = legacySpeechText.Trim()
            }];
        }

        return [];
    }

    private static string ResolveLegacySpeechText(IReadOnlyList<PoiSpeechTextMobileDto> speechTexts, string primaryLanguage, string? fallbackDescription)
    {
        var byPrimary = speechTexts.FirstOrDefault(x => string.Equals(x.LanguageCode, primaryLanguage, StringComparison.OrdinalIgnoreCase));
        if (byPrimary is not null && !string.IsNullOrWhiteSpace(byPrimary.Text))
        {
            return byPrimary.Text;
        }

        var vietnamese = speechTexts.FirstOrDefault(x => string.Equals(x.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase));
        if (vietnamese is not null && !string.IsNullOrWhiteSpace(vietnamese.Text))
        {
            return vietnamese.Text;
        }

        var first = speechTexts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Text));
        if (first is not null)
        {
            return first.Text;
        }

        return fallbackDescription ?? string.Empty;
    }
}
