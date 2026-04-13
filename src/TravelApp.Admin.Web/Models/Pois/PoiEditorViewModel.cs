using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using TravelApp.Admin.Web.Models;

namespace TravelApp.Admin.Web.Models.Pois;

public sealed class PoiEditorViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [StringLength(512)]
    public string? Subtitle { get; set; }

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(128)]
    public string? Category { get; set; }

    [StringLength(512)]
    public string? Location { get; set; }

    [StringLength(1024)]
    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }

    public string? QrContent { get; set; }
    public string? QrImageUrl { get; set; }
    public bool CanShowQr => Id.HasValue && !string.IsNullOrWhiteSpace(QrContent) && !string.IsNullOrWhiteSpace(QrImageUrl);

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double GeofenceRadiusMeters { get; set; } = 100;

    [Required, StringLength(10)]
    public string PrimaryLanguage { get; set; } = "vi";

    [StringLength(4000)]
    public string? SpeechText { get; set; }

    [StringLength(10)]
    public string? SpeechTextLanguageCode { get; set; } = "vi";

    public List<SelectListItem> LanguageOptions { get; set; } = LanguageCodeCatalog.Create();
    public List<PoiLocalizationEditorInput> Localizations { get; set; } = [new()];
    public List<PoiAudioEditorInput> AudioAssets { get; set; } = [new()];
    public List<PoiSpeechTextEditorInput> SpeechTexts { get; set; } = [new()];
}

public sealed class PoiLocalizationEditorInput
{
    [Required, StringLength(10)]
    public string LanguageCode { get; set; } = "vi";

    [Required, StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [StringLength(512)]
    public string? Subtitle { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }
}

public sealed class PoiAudioEditorInput
{
    [Required, StringLength(10)]
    public string LanguageCode { get; set; } = "vi";

    [StringLength(2048)]
    public string? AudioUrl { get; set; }

    [StringLength(4000)]
    public string? Transcript { get; set; }
}

public sealed class PoiSpeechTextEditorInput
{
    [Required, StringLength(10)]
    public string LanguageCode { get; set; } = "vi";

    [StringLength(4000)]
    public string Text { get; set; } = string.Empty;
}
