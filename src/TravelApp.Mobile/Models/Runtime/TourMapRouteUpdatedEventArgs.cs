namespace TravelApp.Models.Runtime;

public sealed class TourMapRouteUpdatedEventArgs : EventArgs
{
    public TourMapRouteUpdatedEventArgs(TourMapRouteSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public TourMapRouteSnapshot Snapshot { get; }
}
