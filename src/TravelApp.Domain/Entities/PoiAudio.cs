namespace TravelApp.Domain.Entities;

public class PoiAudio
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public string? Transcript { get; set; }
    public bool IsGenerated { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Poi Poi { get; set; } = null!;
}
