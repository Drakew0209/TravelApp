using TravelApp.Application.Dtos.Analytics;

namespace TravelApp.Application.Abstractions.Analytics;

public interface IAnalyticsTrackingService
{
    Task TrackAsync(AnalyticsEventRecordDto request, CancellationToken cancellationToken = default);
}
