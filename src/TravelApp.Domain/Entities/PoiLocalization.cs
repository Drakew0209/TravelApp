namespace TravelApp.Domain.Entities;

public class PoiLocalization
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }

    public Poi Poi { get; set; } = null!;
}
