using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface IAnalyticsTrackingService
{
    Task TrackPoiViewedAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default);
    Task TrackTourViewedAsync(int tourId, int? poiId = null, string? languageCode = null, CancellationToken cancellationToken = default);
    Task TrackPoiListenedAsync(PoiMobileDto poi, string? languageCode = null, CancellationToken cancellationToken = default);
    Task TrackTourListenedAsync(int tourId, int? poiId = null, string? languageCode = null, CancellationToken cancellationToken = default);
    Task TrackQrScannedAsync(int poiId, string? payload = null, string? languageCode = null, CancellationToken cancellationToken = default);
}
