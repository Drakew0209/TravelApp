using TravelApp.Models.Contracts;

namespace TravelApp.Models.Runtime;

public sealed class TourMapWaypoint
{
    public int PoiId { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistanceMeters { get; set; }
    public bool IsActive { get; set; }
    public PoiMobileDto Poi { get; set; } = new();
}
