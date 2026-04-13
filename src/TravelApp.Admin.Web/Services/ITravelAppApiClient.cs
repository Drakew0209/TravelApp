using Microsoft.AspNetCore.Http;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Users;
using TravelApp.Application.Dtos.Tours;

namespace TravelApp.Admin.Web.Services;

public interface ITravelAppApiClient
{
    Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleAdminDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto?> CreateUserAsync(UpsertUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(Guid id, UpsertUserRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(string? languageCode = null, CancellationToken cancellationToken = default);
    Task<PoiMobileDto?> GetPoiAsync(int id, string? languageCode = null, CancellationToken cancellationToken = default);
    Task<PoiMobileDto> CreatePoiAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdatePoiAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeletePoiAsync(int id, CancellationToken cancellationToken = default);
    Task<string?> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TourAdminDto>> GetToursAsync(CancellationToken cancellationToken = default);
    Task<TourAdminDto?> GetTourAsync(int id, CancellationToken cancellationToken = default);
    Task<TourAdminDto> CreateTourAsync(UpsertTourRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateTourAsync(int id, UpsertTourRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTourAsync(int id, CancellationToken cancellationToken = default);
}
