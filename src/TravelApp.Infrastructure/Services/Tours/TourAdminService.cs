using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Abstractions.Tours;
using TravelApp.Application.Dtos.Tours;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Services.Tours;

public sealed class TourAdminService : ITourAdminService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ITravelAppDbContext _dbContext;

    public TourAdminService(ITravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TourAdminDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tours = await _dbContext.Tours
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.AnchorPoi)
                .ThenInclude(x => x.Localizations)
            .Include(x => x.AnchorPoi)
                .ThenInclude(x => x.AudioAssets)
            .Include(x => x.TourPois)
                .ThenInclude(x => x.Poi)
                    .ThenInclude(x => x.Localizations)
            .Include(x => x.TourPois)
                .ThenInclude(x => x.Poi)
                    .ThenInclude(x => x.AudioAssets)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return tours.Select(MapTour).ToList();
    }

    public async Task<TourAdminDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.AnchorPoi)
                .ThenInclude(x => x.Localizations)
            .Include(x => x.AnchorPoi)
                .ThenInclude(x => x.AudioAssets)
            .Include(x => x.TourPois)
                .ThenInclude(x => x.Poi)
                    .ThenInclude(x => x.Localizations)
            .Include(x => x.TourPois)
                .ThenInclude(x => x.Poi)
                    .ThenInclude(x => x.AudioAssets)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return tour is null ? null : MapTour(tour);
    }

    public async Task<TourAdminDto> CreateAsync(UpsertTourRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateTourPois(request);

        var tour = new Tour();
        await ApplyAsync(tour, request, isCreate: true, cancellationToken);

        _dbContext.Tours.Add(tour);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(tour.Id, cancellationToken))!;
    }

    public async Task<bool> UpdateAsync(int id, UpsertTourRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateTourPois(request);

        var tour = await _dbContext.Tours
            .Include(x => x.AnchorPoi)
                .ThenInclude(x => x.Localizations)
            .Include(x => x.AnchorPoi)
                .ThenInclude(x => x.AudioAssets)
            .Include(x => x.TourPois)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (tour is null)
        {
            return false;
        }

        await ApplyAsync(tour, request, isCreate: false, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tour is null)
        {
            return false;
        }

        _dbContext.Tours.Remove(tour);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ApplyAsync(Tour tour, UpsertTourRequestDto request, bool isCreate, CancellationToken cancellationToken)
    {
        var anchorPoi = await ResolveAnchorPoiAsync(request, cancellationToken);
        ApplyAnchorPoi(anchorPoi, request);

        if (anchorPoi.Id == 0)
        {
            _dbContext.Pois.Add(anchorPoi);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        tour.AnchorPoi = anchorPoi;
        tour.AnchorPoiId = anchorPoi.Id;
        tour.Name = request.Name.Trim();
        tour.Description = request.Description.Trim();
        tour.CoverImageUrl = NormalizeCoverImageUrl(request.CoverImageUrl ?? request.ImageUrl, request.Name);
        tour.PrimaryLanguage = NormalizeLanguage(request.PrimaryLanguage);
        tour.IsPublished = request.IsPublished;
        tour.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (isCreate)
        {
            tour.CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        ReplaceTourPois(tour, request.Pois, anchorPoi.Id);

        if (string.IsNullOrWhiteSpace(anchorPoi.SpeechText) || anchorPoi.SpeechText != request.Description.Trim())
        {
            anchorPoi.SpeechText = request.Description.Trim();
        }

        anchorPoi.SpeechTextLanguageCode = NormalizeLanguage(request.PrimaryLanguage);
        anchorPoi.SpeechTextsJson = JsonSerializer.Serialize(NormalizeSpeechTexts(request.SpeechTexts, request.Description), JsonOptions);
    }

    private async Task<Poi> ResolveAnchorPoiAsync(UpsertTourRequestDto request, CancellationToken cancellationToken)
    {
        Poi? anchorPoi = null;
        if (request.AnchorPoiId > 0)
        {
            anchorPoi = await _dbContext.Pois
                .Include(x => x.Localizations)
                .Include(x => x.AudioAssets)
                .FirstOrDefaultAsync(x => x.Id == request.AnchorPoiId, cancellationToken);
        }

        anchorPoi ??= new Poi();
        return anchorPoi;
    }

    private static void ApplyAnchorPoi(Poi anchorPoi, UpsertTourRequestDto request)
    {
        anchorPoi.Title = request.Title.Trim();
        anchorPoi.Subtitle = request.Subtitle?.Trim();
        anchorPoi.Description = request.Description.Trim();
        anchorPoi.Category = request.Category?.Trim();
        anchorPoi.Location = request.Location?.Trim();
        anchorPoi.ImageUrl = request.ImageUrl?.Trim();
        anchorPoi.Latitude = request.Latitude;
        anchorPoi.Longitude = request.Longitude;
        anchorPoi.PrimaryLanguage = NormalizeLanguage(request.PrimaryLanguage);

        anchorPoi.Localizations.Clear();
        foreach (var localization in NormalizeLocalizations(request))
        {
            anchorPoi.Localizations.Add(localization);
        }

        anchorPoi.AudioAssets.Clear();
        foreach (var audio in request.AudioAssets.Where(x => !string.IsNullOrWhiteSpace(x.AudioUrl) || !string.IsNullOrWhiteSpace(x.Transcript)))
        {
            anchorPoi.AudioAssets.Add(new PoiAudio
            {
                LanguageCode = NormalizeLanguage(audio.LanguageCode),
                AudioUrl = audio.AudioUrl?.Trim(),
                Transcript = audio.Transcript?.Trim(),
                IsGenerated = audio.IsGenerated,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
    }

    private static IEnumerable<PoiLocalization> NormalizeLocalizations(UpsertTourRequestDto request)
    {
        var items = new List<PoiLocalization>();
        var fallbackLanguage = NormalizeLanguage(request.PrimaryLanguage);

        items.Add(new PoiLocalization
        {
            LanguageCode = fallbackLanguage,
            Title = request.Title.Trim(),
            Subtitle = request.Subtitle?.Trim(),
            Description = request.Description.Trim()
        });

        foreach (var localization in request.SpeechTexts.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
        {
            var languageCode = NormalizeLanguage(localization.LanguageCode);
            if (items.Any(x => string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            items.Add(new PoiLocalization
            {
                LanguageCode = languageCode,
                Title = request.Title.Trim(),
                Subtitle = request.Subtitle?.Trim(),
                Description = request.Description.Trim()
            });
        }

        return items;
    }

    private static List<TourSpeechTextDto> NormalizeSpeechTexts(IEnumerable<TourSpeechTextDto> speechTexts, string fallbackText)
    {
        var items = speechTexts
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => new TourSpeechTextDto
            {
                LanguageCode = NormalizeLanguage(x.LanguageCode),
                Text = x.Text.Trim()
            })
            .ToList();

        if (items.Count == 0 && !string.IsNullOrWhiteSpace(fallbackText))
        {
            items.Add(new TourSpeechTextDto
            {
                LanguageCode = "vi",
                Text = fallbackText.Trim()
            });
        }

        return items;
    }

    private static void ReplaceTourPois(Tour tour, IEnumerable<TourPoiRequestDto> requestPois, int anchorPoiId)
    {
        tour.TourPois.Clear();

        var pois = requestPois
            .Where(x => x.PoiId > 0)
            .DistinctBy(x => x.PoiId)
            .OrderBy(x => x.SortOrder)
            .ToList();

        if (pois.All(x => x.PoiId != anchorPoiId))
        {
            pois.Insert(0, new TourPoiRequestDto { PoiId = anchorPoiId, SortOrder = 1, DistanceFromPreviousMeters = 0 });
        }

        var sortOrder = 1;
        foreach (var poi in pois)
        {
            tour.TourPois.Add(new TourPoi
            {
                PoiId = poi.PoiId,
                SortOrder = sortOrder++,
                DistanceFromPreviousMeters = poi.DistanceFromPreviousMeters
            });
        }
    }

    private static void ValidateTourPois(UpsertTourRequestDto request)
    {
        var poiCount = request.Pois.Count(x => x.PoiId > 0);
        if (poiCount < 2)
        {
            throw new ArgumentException("Tour must contain at least 2 POIs.");
        }
    }

    private static TourAdminDto MapTour(Tour tour)
    {
        var anchorPoi = tour.AnchorPoi;
        return new TourAdminDto
        {
            Id = tour.Id,
            AnchorPoiId = tour.AnchorPoiId,
            Name = tour.Name,
            Title = anchorPoi.Title,
            Subtitle = anchorPoi.Subtitle,
            Description = tour.Description,
            Location = anchorPoi.Location,
            Latitude = anchorPoi.Latitude,
            Longitude = anchorPoi.Longitude,
            Category = anchorPoi.Category,
            ImageUrl = anchorPoi.ImageUrl,
            CoverImageUrl = tour.CoverImageUrl,
            PrimaryLanguage = NormalizeLanguage(tour.PrimaryLanguage),
            IsPublished = tour.IsPublished,
            Pois = tour.TourPois
                .OrderBy(x => x.SortOrder)
                .Select(x => new TourPoiAdminDto
                {
                    PoiId = x.PoiId,
                    PoiTitle = x.Poi.Title,
                    SortOrder = x.SortOrder,
                    DistanceFromPreviousMeters = x.DistanceFromPreviousMeters
                })
                .ToList(),
            AudioAssets = anchorPoi.AudioAssets
                .Select(x => new TourAudioAssetDto
                {
                    LanguageCode = x.LanguageCode,
                    AudioUrl = x.AudioUrl,
                    Transcript = x.Transcript,
                    IsGenerated = x.IsGenerated
                })
                .ToList(),
            SpeechTexts = DeserializeSpeechTexts(anchorPoi.SpeechTextsJson)
        };
    }

    private static List<TourSpeechTextDto> DeserializeSpeechTexts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TourSpeechTextDto>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
    }

    private static string NormalizeCoverImageUrl(string? coverImageUrl, string tourName)
    {
        if (!string.IsNullOrWhiteSpace(coverImageUrl) && !coverImageUrl.Contains("unsplash.com", StringComparison.OrdinalIgnoreCase))
        {
            return coverImageUrl.Trim();
        }

        return $"https://placehold.co/1200x800/png?text={Uri.EscapeDataString(tourName)}";
    }
}
