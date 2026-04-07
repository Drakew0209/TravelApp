using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface ITourRouteCatalogService
{
    Task<TourRouteDto?> GetRouteAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default);
}
