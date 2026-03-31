using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Application.Abstractions.Pois;

public interface IPoiQueryService
{
    Task<PagedResultDto<PoiMobileDto>> GetAllAsync(PoiQueryRequestDto request, CancellationToken cancellationToken = default);
    Task<PoiMobileDto?> GetByIdAsync(int id, string? languageCode, CancellationToken cancellationToken = default);
    Task<PoiMobileDto> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
