using TravelApp.Application.Dtos.Users;

namespace TravelApp.Application.Abstractions.Users;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserAdminDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleAdminDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> CreateAsync(UpsertUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpsertUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
