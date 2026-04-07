using TravelApp.Models.Contracts;

namespace TravelApp.Models.Runtime;

public sealed class TourRoutePlaybackChangedEventArgs : EventArgs
{
    public TourRoutePlaybackChangedEventArgs(TourRouteWaypointDto? waypoint, bool isAutoSelected, LocationSample? userLocation)
    {
        Waypoint = waypoint;
        IsAutoSelected = isAutoSelected;
        UserLocation = userLocation;
    }

    public TourRouteWaypointDto? Waypoint { get; }
    public bool IsAutoSelected { get; }
    public LocationSample? UserLocation { get; }
}
