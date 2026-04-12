using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
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
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        // Use BCrypt to verify password against stored hash
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user.Id, cancellationToken);

        return new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            TokenType: "Bearer",
            UserId: user.Id.ToString(),
            Roles: user.UserRoles
                .Where(x => x.Role is not null)
                .Select(x => x.Role.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        );
    }

    public async Task<AuthResultDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenHash = HashRefreshToken(refreshToken);
        var tokenRecord = await _dbContext.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (tokenRecord is null || tokenRecord.User is null || !tokenRecord.User.IsActive)
        {
            return null;
        }

        if (tokenRecord.RevokedAtUtc.HasValue || tokenRecord.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var newRefreshToken = await IssueRefreshTokenAsync(tokenRecord.User.Id, cancellationToken);
        tokenRecord.RevokedAtUtc = DateTimeOffset.UtcNow;
        tokenRecord.ReplacedByTokenHash = HashRefreshToken(newRefreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            AccessToken: GenerateAccessToken(tokenRecord.User),
            RefreshToken: newRefreshToken,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            TokenType: "Bearer",
            UserId: tokenRecord.User.Id.ToString(),
            Roles: tokenRecord.User.UserRoles
                .Where(x => x.Role is not null)
                .Select(x => x.Role.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var tokenRecord = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (tokenRecord is null || tokenRecord.RevokedAtUtc.HasValue)
        {
            return;
        }

        tokenRecord.RevokedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
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
        var key = Encoding.ASCII.GetBytes(GetRequiredJwtSetting("Jwt:Secret"));
        var issuer = GetRequiredJwtSetting("Jwt:Issuer");
        var audience = GetRequiredJwtSetting("Jwt:Audience");

        var claims = new List<Claim>
        {
            new Claim("sub", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("email_verified", "true"),
        };

        foreach (var roleName in user.UserRoles.Where(x => x.Role is not null).Select(x => x.Role.Name).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var refreshToken = GenerateRefreshToken();

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashRefreshToken(refreshToken),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private string GetRequiredJwtSetting(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required JWT configuration value '{key}'.");
        }

        return value;
    }
}
