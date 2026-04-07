using TravelApp.Domain.Entities;

namespace TravelApp.Application.Abstractions.Auth;

public interface IAuthService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    /// <returns>Auth result with access token, or null if authentication fails</returns>
    Task<AuthResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<AuthResultDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find user by email
    /// </summary>
    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public record AuthResultDto(
    string AccessToken,
    string? RefreshToken = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string TokenType = "Bearer",
    string? UserId = null,
    IReadOnlyList<string>? Roles = null);

public record UserProfileDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName = "");
