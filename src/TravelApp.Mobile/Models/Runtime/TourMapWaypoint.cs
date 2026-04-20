using TravelApp.Models.Contracts;
using TravelApp.Resources.Strings;

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

    public string StopLabelText => $"{AppStrings.RouteStopsLabel} {SortOrder}";
    public string DistanceText => DistanceMeters is null ? string.Empty : $"{AppStrings.RouteDistancePrefix} {DistanceMeters:F0} m";
    public string PlayingBadgeText => AppStrings.AudioNowPlaying;
}
