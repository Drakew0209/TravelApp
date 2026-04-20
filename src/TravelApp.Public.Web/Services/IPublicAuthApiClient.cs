using TravelApp.Application.Abstractions.Auth;

namespace TravelApp.Public.Web.Services;

public interface IPublicAuthApiClient
{
    Task<AuthResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResultDto?> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default);
    Task<AuthResultDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
}
