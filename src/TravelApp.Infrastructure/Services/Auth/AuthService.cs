using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TravelApp.Application.Abstractions.Auth;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ITravelAppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(ITravelAppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<AuthResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await FindUserByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        // Use BCrypt to verify password against stored hash
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        return new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            TokenType: "Bearer",
            UserId: user.Id.ToString(),
            Roles: new[] { "User" }
        );
    }

    public async Task<AuthResultDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // For demo purposes, accept any refresh token
        // In production, validate against stored refresh tokens
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        // Get the user from the current token (in a real app, decode and verify)
        var newAccessToken = GenerateAccessToken(new User { Id = Guid.NewGuid(), Email = "user@example.com" });
        var newRefreshToken = GenerateRefreshToken();

        return new AuthResultDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            TokenType: "Bearer"
        );
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        return new UserProfileDto(
            Id: user.Id,
            UserName: user.UserName,
            Email: user.Email,
            FullName: user.UserName
        );
    }

    public async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    private string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-secret-key-must-be-at-least-32-characters-long");

        var claims = new List<Claim>
        {
            new Claim("sub", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("email_verified", "true"),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"] ?? "TravelApp",
            Audience = _configuration["Jwt:Audience"] ?? "TravelAppUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber);
    }
}
