using TravelApp.Application.Dtos.Analytics;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Tours;

namespace TravelApp.Public.Web.Services;

public interface ITravelAppPublicApiClient
{
    Task<PoiMobileDto?> GetPoiAsync(int id, string? languageCode = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourRouteDto>> GetPublishedToursAsync(string? languageCode = null, CancellationToken cancellationToken = default);
    Task<bool> TrackEventAsync(AnalyticsEventRecordDto request, CancellationToken cancellationToken = default);
}
