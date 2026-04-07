namespace TravelApp.Models.Contracts;

public sealed class TourRouteDto
{
    public int Id { get; set; }
    public int AnchorPoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string PrimaryLanguage { get; set; } = "en";
    public double TotalDistanceMeters { get; set; }
    public IReadOnlyList<TourRouteWaypointDto> Waypoints { get; set; } = [];
}

public sealed class TourRouteWaypointDto
{
    public int SortOrder { get; set; }
    public double? DistanceFromPreviousMeters { get; set; }
    public PoiMobileDto Poi { get; set; } = new();
}
