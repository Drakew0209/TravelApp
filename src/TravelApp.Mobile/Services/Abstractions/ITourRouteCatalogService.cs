using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface ITourRouteCatalogService
{
    Task<TourRouteDto?> GetRouteAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourRouteDto>> GetAllRoutesAsync(string? languageCode = null, CancellationToken cancellationToken = default);
    Task<PoiDto?> ResolvePoiAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default);
}
