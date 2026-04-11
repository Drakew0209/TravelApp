using TravelApp.Application.Dtos.Tours;

namespace TravelApp.Application.Abstractions.Tours;

public interface ITourAdminService
{
    Task<IReadOnlyList<TourAdminDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TourAdminDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TourAdminDto> CreateAsync(UpsertTourRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, UpsertTourRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
