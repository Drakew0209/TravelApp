namespace TravelApp.Domain.Entities;

public class Tour
{
    public int Id { get; set; }
    public int AnchorPoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string PrimaryLanguage { get; set; } = "en";
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Poi AnchorPoi { get; set; } = null!;
    public ICollection<TourPoi> TourPois { get; set; } = new List<TourPoi>();
}
