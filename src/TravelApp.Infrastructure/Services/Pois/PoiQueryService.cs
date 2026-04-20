using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Utilities;
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

    public async Task<int> BackfillSpeechTextsAsync(CancellationToken cancellationToken = default)
    {
        var pois = await _dbContext.Pois
            .Include(x => x.Localizations)
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        foreach (var poi in pois)
        {
            if (BackfillSpeechTexts(poi))
            {
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return updatedCount;
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
        var speech = ResolveSpeechText(speechTexts, speechLanguage, primaryLanguage, poi.SpeechText);

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

    private static bool BackfillSpeechTexts(Poi poi)
    {
        var existingSpeechTexts = DeserializeSpeechTexts(poi.SpeechTextsJson);
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(poi.SpeechText))
        {
            var legacyLanguage = NormalizeLanguageCode(string.IsNullOrWhiteSpace(poi.SpeechTextLanguageCode) ? poi.PrimaryLanguage : poi.SpeechTextLanguageCode);
            if (!string.IsNullOrWhiteSpace(legacyLanguage))
            {
                merged[legacyLanguage] = poi.SpeechText.Trim();
            }
        }

        foreach (var speechText in existingSpeechTexts)
        {
            var languageCode = NormalizeLanguageCode(speechText.LanguageCode);
            if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(speechText.Text) || merged.ContainsKey(languageCode))
            {
                continue;
            }

            merged[languageCode] = speechText.Text.Trim();
        }

        foreach (var languageCode in GetBackfillLanguageCandidates(poi))
        {
            if (merged.ContainsKey(languageCode))
            {
                continue;
            }

            var fallback = BuildFallbackSpeechText(poi, languageCode);
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                merged[languageCode] = fallback;
            }
        }

        if (merged.Count == 0)
        {
            return false;
        }

        var normalizedPrimaryLanguage = NormalizeLanguageCode(poi.SpeechTextLanguageCode);
        if (string.IsNullOrWhiteSpace(normalizedPrimaryLanguage) || !merged.ContainsKey(normalizedPrimaryLanguage))
        {
            normalizedPrimaryLanguage = NormalizeLanguageCode(poi.PrimaryLanguage);
        }

        if (string.IsNullOrWhiteSpace(normalizedPrimaryLanguage) || !merged.ContainsKey(normalizedPrimaryLanguage))
        {
            normalizedPrimaryLanguage = merged.Keys.First();
        }

        var primarySpeechText = merged[normalizedPrimaryLanguage];
        var orderedSpeechTexts = merged
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PoiSpeechTextMobileDto
            {
                LanguageCode = x.Key,
                Text = x.Value
            })
            .ToList();

        var newJson = JsonSerializer.Serialize(orderedSpeechTexts);
        var changed = !string.Equals(poi.SpeechTextsJson, newJson, StringComparison.Ordinal) ||
                      !string.Equals(poi.SpeechText, primarySpeechText, StringComparison.Ordinal) ||
                      !string.Equals(NormalizeLanguageCode(poi.SpeechTextLanguageCode), normalizedPrimaryLanguage, StringComparison.OrdinalIgnoreCase);

        if (!changed)
        {
            return false;
        }

        poi.SpeechTextsJson = newJson;
        poi.SpeechText = primarySpeechText;
        poi.SpeechTextLanguageCode = normalizedPrimaryLanguage;
        poi.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return true;
    }

    private static IEnumerable<string> GetBackfillLanguageCandidates(Poi poi)
    {
        var languages = new[]
        {
            poi.PrimaryLanguage,
            poi.SpeechTextLanguageCode,
            "vi-VN",
            "en-US",
            "ja-JP",
            "de-DE"
        };

        foreach (var localizationLanguage in poi.Localizations.Select(x => x.LanguageCode))
        {
            languages = languages.Append(localizationLanguage).ToArray();
        }

        return languages
            .Select(NormalizeLanguageCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildFallbackSpeechText(Poi poi, string languageCode)
    {
        var localization = poi.Localizations.FirstOrDefault(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), languageCode, StringComparison.OrdinalIgnoreCase))
                           ?? poi.Localizations.FirstOrDefault(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), NormalizeLanguageCode(poi.PrimaryLanguage), StringComparison.OrdinalIgnoreCase))
                           ?? poi.Localizations.FirstOrDefault();

        var parts = new[]
            {
                localization?.Title ?? poi.Title,
                localization?.Subtitle ?? poi.Subtitle,
                localization?.Description ?? poi.Description
            }
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(". ", parts);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode))
            ? "en-US"
            : LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
    }

    private static void ApplyRequest(Poi poi, UpsertPoiRequestDto request)
    {
        poi.Title = request.Title;
        poi.Subtitle = request.Subtitle;
        var speechTexts = NormalizeSpeechTexts(request.SpeechTexts, request.SpeechText, request.SpeechTextLanguageCode, request.PrimaryLanguage);
        var primarySpeechLanguage = ResolvePrimarySpeechLanguage(request.SpeechTextLanguageCode, request.PrimaryLanguage, speechTexts);
        var primarySpeechText = ResolvePrimarySpeechText(request.Description, request.SpeechText, speechTexts, primarySpeechLanguage);

        poi.Description = primarySpeechText;
        poi.Category = request.Category;
        poi.Location = request.Location;
        poi.ImageUrl = request.ImageUrl;
        poi.Latitude = request.Latitude;
        poi.Longitude = request.Longitude;
        poi.GeofenceRadiusMeters = request.GeofenceRadiusMeters;
        poi.PrimaryLanguage = NormalizeLanguageCode(request.PrimaryLanguage);
        poi.SpeechTextsJson = JsonSerializer.Serialize(speechTexts);
        poi.SpeechText = primarySpeechText;
        poi.SpeechTextLanguageCode = primarySpeechLanguage;

        if (request.Localizations.Count > 0)
        {
            poi.Localizations.Clear();
            foreach (var localization in request.Localizations)
            {
                var languageCode = NormalizeLanguageCode(localization.LanguageCode);
                poi.Localizations.Add(new PoiLocalization
                {
                    LanguageCode = languageCode,
                    Title = request.Title,
                    Subtitle = request.Subtitle,
                    Description = ResolveSpeechTextForLanguage(speechTexts, languageCode, localization.Description, primarySpeechText)
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
        string? legacySpeechText)
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

    private static string ResolveLegacySpeechText(IReadOnlyList<PoiSpeechTextMobileDto> speechTexts, string primaryLanguage, string? legacySpeechText, string? fallbackDescription)
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

        if (!string.IsNullOrWhiteSpace(legacySpeechText)
            && !string.Equals(legacySpeechText.Trim(), fallbackDescription?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return legacySpeechText.Trim();
        }

        return string.Empty;
    }

    private static string ResolvePrimarySpeechText(string? description, string? legacySpeechText, IReadOnlyList<PoiSpeechTextMobileDto> speechTexts, string primaryLanguage)
    {
        var selected = speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), primaryLanguage, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Text))
                       ?? speechTexts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Text));
        if (selected is not null)
        {
            return selected.Text.Trim();
        }

        if (!string.IsNullOrWhiteSpace(legacySpeechText))
        {
            return legacySpeechText.Trim();
        }

        return description?.Trim() ?? string.Empty;
    }

    private static string ResolvePrimarySpeechLanguage(string? preferredLanguage, string? primaryLanguage, IReadOnlyList<PoiSpeechTextMobileDto> speechTexts)
    {
        var preferred = NormalizeLanguageCode(preferredLanguage);
        if (!string.IsNullOrWhiteSpace(preferred) && speechTexts.Any(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), preferred, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Text)))
        {
            return preferred;
        }

        var primary = NormalizeLanguageCode(primaryLanguage);
        if (!string.IsNullOrWhiteSpace(primary) && speechTexts.Any(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), primary, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Text)))
        {
            return primary;
        }

        return speechTexts.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Text))?.LanguageCode is { } languageCode
            ? NormalizeLanguageCode(languageCode)
            : primary;
    }

    private static string ResolveSpeechTextForLanguage(IReadOnlyList<PoiSpeechTextMobileDto> speechTexts, string languageCode, string? fallbackText, string primarySpeechText)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        var selected = speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguageCode(x.LanguageCode), normalized, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Text));
        if (selected is not null)
        {
            return selected.Text.Trim();
        }

        return !string.IsNullOrWhiteSpace(fallbackText) ? fallbackText.Trim() : primarySpeechText;
    }
}
