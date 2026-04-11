using System.ComponentModel.DataAnnotations;

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

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double GeofenceRadiusMeters { get; set; } = 100;

    [Required, StringLength(10)]
    public string PrimaryLanguage { get; set; } = "vi";

    [StringLength(4000)]
    public string? SpeechText { get; set; }

    [StringLength(10)]
    public string? SpeechTextLanguageCode { get; set; } = "vi";
}
