using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TravelApp.Admin.Web.Models.Tours;

public sealed class TourEditorViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(256)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(512)]
    public string? Subtitle { get; set; }

    [Required, StringLength(256)]
    [Display(Name = "Tour name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(1024)]
    [Display(Name = "Cover image URL")]
    public string? CoverImageUrl { get; set; }

    [Required, StringLength(10)]
    [Display(Name = "Primary language")]
    public string PrimaryLanguage { get; set; } = "vi";

    public bool IsPublished { get; set; } = true;

    public int? TourId { get; set; }

    [Required]
    [Display(Name = "Anchor POI")]
    public int AnchorPoiId { get; set; }

    [StringLength(512)]
    [Display(Name = "Location")]
    public string? Location { get; set; }

    [Display(Name = "Latitude")]
    public double? Latitude { get; set; }

    [Display(Name = "Longitude")]
    public double? Longitude { get; set; }

    [StringLength(128)]
    public string? Category { get; set; }

    [StringLength(1024)]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    public string AnchorPoiDetailsJson { get; set; } = "[]";
    public List<SelectListItem> AvailablePois { get; set; } = [];

    public List<TourPoiEditorInput> Pois { get; set; } = [new()];
    public List<TourAudioEditorInput> AudioAssets { get; set; } = [new()];
    public List<TourSpeechTextEditorInput> SpeechTexts { get; set; } = [new()];
}

public sealed class TourPoiEditorInput
{
    [Required]
    public int PoiId { get; set; }

    [Range(1, 1000)]
    public int SortOrder { get; set; } = 1;

    public double? DistanceFromPreviousMeters { get; set; }
}

public sealed class TourAudioEditorInput
{
    [Required, StringLength(10)]
    public string LanguageCode { get; set; } = "vi";

    [StringLength(2048)]
    public string? AudioUrl { get; set; }

    [StringLength(4000)]
    public string? Transcript { get; set; }
}

public sealed class TourSpeechTextEditorInput
{
    [Required, StringLength(10)]
    public string LanguageCode { get; set; } = "vi";

    [StringLength(4000)]
    public string Text { get; set; } = string.Empty;
}
