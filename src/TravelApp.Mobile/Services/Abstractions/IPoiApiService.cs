using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface IPoiApiService
{
    Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        string? languageCode,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
