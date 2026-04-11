using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Application.Dtos.Tours;

public sealed class TourAdminDto
{
    public int Id { get; set; }
    public int AnchorPoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string PrimaryLanguage { get; set; } = "en";
    public bool IsPublished { get; set; }
    public List<TourPoiAdminDto> Pois { get; set; } = [];
    public List<TourAudioAssetDto> AudioAssets { get; set; } = [];
    public List<TourSpeechTextDto> SpeechTexts { get; set; } = [];
}

public sealed class UpsertTourRequestDto
{
    public int AnchorPoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string PrimaryLanguage { get; set; } = "en";
    public bool IsPublished { get; set; } = true;
    public List<TourPoiRequestDto> Pois { get; set; } = [];
    public List<TourAudioAssetDto> AudioAssets { get; set; } = [];
    public List<TourSpeechTextDto> SpeechTexts { get; set; } = [];
}

public sealed class TourPoiAdminDto
{
    public int PoiId { get; set; }
    public string PoiTitle { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public double? DistanceFromPreviousMeters { get; set; }
}

public sealed class TourPoiRequestDto
{
    public int PoiId { get; set; }
    public int SortOrder { get; set; }
    public double? DistanceFromPreviousMeters { get; set; }
}

public sealed class TourAudioAssetDto
{
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public string? Transcript { get; set; }
    public bool IsGenerated { get; set; }
}

public sealed class TourSpeechTextDto
{
    public string LanguageCode { get; set; } = "en";
    public string Text { get; set; } = string.Empty;
}
