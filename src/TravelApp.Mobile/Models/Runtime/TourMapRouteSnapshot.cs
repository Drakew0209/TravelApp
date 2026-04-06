namespace TravelApp.Models.Runtime;

public sealed class TourMapRouteSnapshot
{
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public LocationSample? CurrentLocation { get; set; }
    public IReadOnlyList<TourMapWaypoint> Waypoints { get; set; } = [];
}
