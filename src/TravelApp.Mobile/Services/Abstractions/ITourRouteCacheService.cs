using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface ITourRouteCacheService
{
    Task<TourRouteDto?> GetAsync(int anchorPoiId, string? languageCode = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourRouteDto>> GetAllAsync(string? languageCode = null, CancellationToken cancellationToken = default);
    Task SaveAsync(TourRouteDto route, CancellationToken cancellationToken = default);
}
