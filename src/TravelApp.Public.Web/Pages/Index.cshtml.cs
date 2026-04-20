using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelApp.Public.Web;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Tours;
using TravelApp.Application.Utilities;
using TravelApp.Public.Web.Services;

namespace TravelApp.Public.Web.Pages;

public sealed class IndexModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITravelAppPublicApiClient _apiClient;

    [BindProperty(SupportsGet = true, Name = "poiId")]
    public int? PoiId { get; set; }

    [BindProperty(SupportsGet = true, Name = "tourId")]
    public int? TourId { get; set; }

    [BindProperty(SupportsGet = true, Name = "lang")]
    public string? LanguageCode { get; set; }

    public PoiMobileDto? Poi { get; private set; }
    public TourRouteDto? Tour { get; private set; }
    public IReadOnlyList<PublicLanguageOption> Languages { get; private set; } = [];
    public IReadOnlyList<TourRouteWaypointDto> Waypoints { get; private set; } = [];
    public IReadOnlyList<PublicMapPoint> MapPoints { get; private set; } = [];
    public string SelectedLanguageCode { get; private set; } = "vi-VN";
    public string? AudioNoticeMessage { get; private set; }
    public bool HasFallbackAudioBadge { get; private set; }
    public string FallbackAudioBadgeText { get; private set; } = "fallback";
    public string PageTitle { get; private set; } = "TravelApp Public Audio";
    public string PageSubtitle { get; private set; } = string.Empty;
    public string CurrentLanguageDisplayText { get; private set; } = string.Empty;
    public IReadOnlyList<PublicCultureOption> Cultures { get; private set; } = [];
    public string? CurrentAudioUrl { get; private set; }
    public string? CurrentSpeechText { get; private set; }
    public string PageStateJson { get; private set; } = "{}";
    public string FeaturedToursJson { get; private set; } = "[]";

    public IndexModel(ITravelAppPublicApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var requestedLanguage = NormalizeLanguage(LanguageCode);
        if (string.IsNullOrWhiteSpace(requestedLanguage))
        {
            requestedLanguage = NormalizeLanguage(CultureInfo.CurrentUICulture.Name);
        }
        var languageCandidates = BuildLanguageCandidates(requestedLanguage);
        IReadOnlyList<TourRouteDto> tours = [];
        var resolvedLanguage = requestedLanguage;

        foreach (var candidate in languageCandidates)
        {
            var candidateTours = await _apiClient.GetPublishedToursAsync(candidate, cancellationToken);
            var candidateTour = ResolveTour(candidateTours, PoiId, TourId);
            PoiMobileDto? candidatePoi = null;
            if (PoiId.HasValue)
            {
                candidatePoi = await _apiClient.GetPoiAsync(PoiId.Value, candidate, cancellationToken);
            }

            var hasResolvedContent = candidatePoi is not null || candidateTour is not null;
            var hasStandaloneContent = !PoiId.HasValue && !TourId.HasValue && candidateTours.Count > 0;

            if (hasResolvedContent || hasStandaloneContent)
            {
                resolvedLanguage = candidate;
                tours = candidateTours;
                Tour = candidateTour;
                Poi = candidatePoi;
                break;
            }
        }

        FeaturedToursJson = JsonSerializer.Serialize(tours, JsonOptions);
        Cultures = BuildCultures(resolvedLanguage);
        CurrentLanguageDisplayText = DisplayLanguage(resolvedLanguage);

        if (Poi is null && Tour is not null)
        {
            Poi = ResolveTourPoi(Tour, PoiId) ?? Tour.Waypoints.FirstOrDefault()?.Poi;
        }

        if (Poi is null && Tour is null)
        {
            PageTitle = PublicText.T("TravelApp Public Audio", "TravelApp 公開オーディオ", "TravelApp Public Audio", "TravelApp Public Audio");
            PageSubtitle = PublicText.T("Khám phá POI, tour, audio và lịch sử nghe ngay trên web public.", "公開 POI、ツアー、音声、視聴履歴をそのまま公開 Web で体験できます。", "Erkunden Sie POIs, Touren, Audio und den Hörverlauf direkt im Public Web.", "Explore POIs, tours, audio, and listening history directly in the public web.");
            PageStateJson = JsonSerializer.Serialize(new PublicPageState(PoiId, TourId, resolvedLanguage, PageTitle, PageSubtitle, null, null, null, null, null, [], [], false), JsonOptions);
            return;
        }

        SelectedLanguageCode = resolvedLanguage;
        Languages = BuildLanguages(Poi, Tour, resolvedLanguage);
        Waypoints = Tour?.Waypoints ?? [];
        MapPoints = BuildMapPoints(Poi, Tour, resolvedLanguage);
        var contentSelection = ResolveContentForLanguage(Poi, resolvedLanguage);
        PageTitle = contentSelection.Title ?? Tour?.Name ?? PublicText.T("TravelApp Public Audio", "TravelApp 公開オーディオ", "TravelApp Public Audio", "TravelApp Public Audio");
        PageSubtitle = contentSelection.Subtitle ?? Tour?.Description ?? PublicText.T("Nghe audio, lưu lịch sử và xem nội dung public.", "音声を聴いて履歴を保存し、公開コンテンツを表示します。", "Audio anhören, Verlauf speichern und öffentliche Inhalte ansehen.", "Listen to audio, save history, and view public content.");
        var speechSelection = ResolveSpeechTextWithFallback(Poi, resolvedLanguage, contentSelection);
        HasFallbackAudioBadge = speechSelection.IsFallback;

        CurrentSpeechText = speechSelection.Text;
        CurrentAudioUrl = null;
        AudioNoticeMessage = BuildAudioNotice(Poi, requestedLanguage, speechSelection);

        PageStateJson = JsonSerializer.Serialize(new PublicPageState(
            Poi?.Id,
            Tour?.Id,
            resolvedLanguage,
            PageTitle,
            PageSubtitle,
            Poi?.Location,
            Poi?.ImageUrl,
            CurrentAudioUrl,
            CurrentSpeechText,
            AudioNoticeMessage,
            BuildLanguagePayload(Languages),
            MapPoints,
            Tour is not null), JsonOptions);
    }

    private static IReadOnlyList<string> BuildLanguageCandidates(string requestedLanguage)
    {
        var candidates = new[]
        {
            requestedLanguage,
            NormalizeLanguage(CultureInfo.CurrentUICulture.Name),
            "vi-VN",
            "en-US",
            "ja-JP",
            "de-DE"
        };

        return candidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string BuildLanguageUrl(string languageCode)
    {
        return BuildRootUrl(Poi?.Id ?? PoiId, Tour?.Id ?? TourId, languageCode);
    }

    public string BuildPoiUrl(int poiId)
    {
        return BuildRootUrl(poiId, Tour?.Id ?? TourId, SelectedLanguageCode);
    }

    private static string BuildRootUrl(int? poiId, int? tourId, string? languageCode)
    {
        var query = new List<string>(3);

        if (poiId.HasValue)
        {
            query.Add($"poiId={Uri.EscapeDataString(poiId.Value.ToString(CultureInfo.InvariantCulture))}");
        }

        if (tourId.HasValue)
        {
            query.Add($"tourId={Uri.EscapeDataString(tourId.Value.ToString(CultureInfo.InvariantCulture))}");
        }

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            query.Add($"lang={Uri.EscapeDataString(languageCode)}");
        }

        return query.Count == 0 ? "/" : $"/?{string.Join('&', query)}";
    }

    public bool IsCurrentPoi(int poiId)
    {
        return Poi?.Id == poiId;
    }

    private IReadOnlyList<PublicMapPoint> BuildMapPoints(PoiMobileDto? poi, TourRouteDto? tour, string selectedLanguage)
    {
        if (tour is not null)
        {
            return tour.Waypoints
                .Where(x => HasCoordinates(x.Poi.Latitude, x.Poi.Longitude))
                .Select(x => new PublicMapPoint(
                    x.Poi.Id,
                    ResolveContentForLanguage(x.Poi, selectedLanguage).Title ?? x.Poi.Title,
                    ResolveContentForLanguage(x.Poi, selectedLanguage).Subtitle ?? x.Poi.Subtitle,
                    x.Poi.Latitude,
                    x.Poi.Longitude,
                    x.SortOrder,
                    Poi?.Id == x.Poi.Id,
                    BuildPoiUrl(x.Poi.Id),
                    null,
                    ResolveSpeechTextWithFallback(x.Poi, selectedLanguage, ResolveContentForLanguage(x.Poi, selectedLanguage)).Text))
                .ToList();
        }

        if (poi is not null && HasCoordinates(poi.Latitude, poi.Longitude))
        {
            var content = ResolveContentForLanguage(poi, selectedLanguage);
            return [new PublicMapPoint(
                poi.Id,
                content.Title ?? poi.Title,
                content.Subtitle ?? poi.Subtitle,
                poi.Latitude,
                poi.Longitude,
                1,
                true,
                BuildPoiUrl(poi.Id),
                null,
                ResolveSpeechTextWithFallback(poi, selectedLanguage, content).Text)];
        }

        return [];
    }

    private static bool HasCoordinates(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsNaN(longitude))
        {
            return false;
        }

        return latitude is >= -90 and <= 90
               && longitude is >= -180 and <= 180
               && (Math.Abs(latitude) > 0.00001 || Math.Abs(longitude) > 0.00001);
    }

    private static TourRouteDto? ResolveTour(IReadOnlyList<TourRouteDto> tours, int? poiId, int? tourId)
    {
        if (tourId.HasValue)
        {
            return tours.FirstOrDefault(x => x.Id == tourId.Value)
                   ?? tours.FirstOrDefault(x => x.AnchorPoiId == poiId);
        }

        if (poiId.HasValue)
        {
            return tours.FirstOrDefault(x => x.AnchorPoiId == poiId.Value || x.Waypoints.Any(w => w.Poi.Id == poiId.Value));
        }

        return null;
    }

    private static PoiMobileDto? ResolveTourPoi(TourRouteDto tour, int? poiId)
    {
        if (!poiId.HasValue)
        {
            return null;
        }

        return tour.Waypoints.FirstOrDefault(x => x.Poi.Id == poiId.Value)?.Poi
               ?? (tour.AnchorPoiId == poiId.Value ? tour.Waypoints.FirstOrDefault()?.Poi : null);
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized;
    }

    private static IReadOnlyList<PublicLanguageOption> BuildLanguages(PoiMobileDto? poi, TourRouteDto? tour, string selectedLanguage)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var options = new List<PublicLanguageOption>();

        foreach (var code in GetAvailableLanguages(poi, tour))
        {
            if (codes.Add(code))
            {
                options.Add(new PublicLanguageOption(code, DisplayLanguage(code), string.Equals(code, selectedLanguage, StringComparison.OrdinalIgnoreCase)));
            }
        }

        return options;
    }

    private static IReadOnlyList<string> BuildLanguagePayload(IReadOnlyList<PublicLanguageOption> languages)
    {
        return languages.Select(x => x.Code).ToList();
    }

    private static IReadOnlyList<PublicCultureOption> BuildCultures(string selectedCulture)
    {
        var cultures = new[] { "vi-VN", "en-US", "ja-JP", "de-DE" };
        return cultures
            .Select(code => new PublicCultureOption(code, DisplayLanguage(code), string.Equals(code, selectedCulture, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static IReadOnlyList<string> GetAvailableLanguages(PoiMobileDto? poi, TourRouteDto? tour = null)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (poi is not null)
        {
            foreach (var localization in poi.Localizations)
            {
                AddLanguageCode(codes, localization.LanguageCode);
            }

            foreach (var speechText in poi.SpeechTexts)
            {
                AddLanguageCode(codes, speechText.LanguageCode);
            }

            if (codes.Count == 0)
            {
                AddLanguageCode(codes, poi.PrimaryLanguage);
            }
        }

        if (tour is not null && codes.Count == 0)
        {
            AddLanguageCode(codes, tour.PrimaryLanguage);
        }

        return codes.ToList();
    }

    private static void AddLanguageCode(ICollection<string> codes, string? languageCode)
    {
        var normalized = NormalizeLanguage(languageCode);
        if (!string.IsNullOrWhiteSpace(normalized) && !codes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            codes.Add(normalized);
        }
    }

    private static ResolvedContent ResolveContentForLanguage(PoiMobileDto? poi, string languageCode)
    {
        if (poi is null)
        {
            return new ResolvedContent(null, null, null, false, null);
        }

        var normalized = NormalizeLanguage(languageCode);
        var localization = poi.Localizations.FirstOrDefault(x => string.Equals(NormalizeLanguage(x.LanguageCode), normalized, StringComparison.OrdinalIgnoreCase))
                           ?? poi.Localizations.FirstOrDefault(x => string.Equals(NormalizeLanguage(x.LanguageCode), NormalizeLanguage(poi.PrimaryLanguage), StringComparison.OrdinalIgnoreCase))
                           ?? poi.Localizations.FirstOrDefault(x => string.Equals(NormalizeLanguage(x.LanguageCode), "en", StringComparison.OrdinalIgnoreCase))
                           ?? poi.Localizations.FirstOrDefault();

        if (localization is not null)
        {
            return new ResolvedContent(
                localization.Title?.Trim(),
                localization.Subtitle?.Trim(),
                localization.Description?.Trim(),
                !string.Equals(NormalizeLanguage(localization.LanguageCode), normalized, StringComparison.OrdinalIgnoreCase),
                NormalizeLanguage(localization.LanguageCode));
        }

        return new ResolvedContent(poi.Title, poi.Subtitle, poi.Description, true, NormalizeLanguage(poi.PrimaryLanguage));
    }

    private static (string? Text, string? LanguageCode, bool IsFallback) ResolveSpeechTextWithFallback(PoiMobileDto? poi, string languageCode, ResolvedContent content)
    {
        if (poi is null)
        {
            return (null, null, false);
        }

        var normalized = NormalizeLanguage(languageCode);
        var exactSpeech = poi.SpeechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguage(x.LanguageCode), normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exactSpeech?.Text))
        {
            return (exactSpeech.Text.Trim(), NormalizeLanguage(exactSpeech.LanguageCode), false);
        }

        var text = !string.IsNullOrWhiteSpace(content.Description)
            ? content.Description
            : !string.IsNullOrWhiteSpace(content.Subtitle)
                ? $"{content.Title}. {content.Subtitle}"
                : content.Title;

        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null, false);
        }

        return (text, content.LanguageCode ?? normalized, true);
    }

    private static string? BuildAudioNotice(PoiMobileDto? poi, string requestedLanguage, (string? Text, string? LanguageCode, bool IsFallback) speechSelection)
    {
        if (poi is null)
        {
            return null;
        }

        var requested = NormalizeLanguage(requestedLanguage).ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(speechSelection.Text))
        {
            return "Chưa có nội dung TTS cho ngôn ngữ này.";
        }

        return speechSelection.IsFallback
            ? $"Chưa có audio cho {requested}; đang dùng TTS dự phòng {NormalizeLanguage(speechSelection.LanguageCode).ToUpperInvariant()}."
            : $"Đang dùng TTS cho {requested}.";
    }

    private sealed record ResolvedContent(string? Title, string? Subtitle, string? Description, bool IsFallback, string? LanguageCode);

    private static string DisplayLanguage(string? languageCode)
    {
        var normalized = NormalizeLanguage(languageCode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "--";
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            return string.IsNullOrWhiteSpace(culture.NativeName)
                ? normalized.ToUpperInvariant()
                : $"{culture.NativeName} ({normalized.ToUpperInvariant()})";
        }
        catch
        {
            return normalized.ToUpperInvariant();
        }
    }

    public sealed record PublicLanguageOption(string Code, string DisplayName, bool IsSelected);
    public sealed record PublicCultureOption(string Code, string DisplayName, bool IsSelected);

    public sealed record PublicPageState(
        int? PoiId,
        int? TourId,
        string LanguageCode,
        string Title,
        string? Subtitle,
        string? Location,
        string? ImageUrl,
        string? AudioUrl,
        string? SpeechText,
        string? AudioNoticeMessage,
        IReadOnlyList<string> Languages,
        IReadOnlyList<PublicMapPoint> MapPoints,
        bool HasTour);

    public sealed record PublicMapPoint(
        int PoiId,
        string Title,
        string? Subtitle,
        double Latitude,
        double Longitude,
        int SortOrder,
        bool IsActive,
        string Link,
        string? AudioUrl,
        string? SpeechText);
}
