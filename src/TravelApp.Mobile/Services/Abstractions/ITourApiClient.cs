using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface ITourApiClient
{
    Task<TourRouteDto?> GetByAnchorPoiIdAsync(int anchorPoiId, string? languageCode = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourRouteDto>> GetAllAsync(string? languageCode = null, CancellationToken cancellationToken = default);
}
