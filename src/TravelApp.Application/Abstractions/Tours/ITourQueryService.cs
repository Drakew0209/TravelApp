using TravelApp.Application.Dtos.Tours;

namespace TravelApp.Application.Abstractions.Tours;

public interface ITourQueryService
{
    Task<TourRouteDto?> GetByAnchorPoiIdAsync(int anchorPoiId, string? languageCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourRouteDto>> GetAllPublishedAsync(string? languageCode, CancellationToken cancellationToken = default);
}
