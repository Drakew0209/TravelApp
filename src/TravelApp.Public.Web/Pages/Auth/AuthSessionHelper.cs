using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TravelApp.Application.Abstractions.Auth;

namespace TravelApp.Public.Web.Pages.Auth;

internal static class AuthSessionHelper
{
    private const string AccessTokenName = "access_token";
    private const string RefreshTokenName = "refresh_token";
    private const string TokenTypeName = "token_type";
    private const string ExpiresAtName = "expires_at";

    public static ClaimsPrincipal BuildPrincipal(AuthResultDto result, string email, string fullName)
    {
        var claims = new List<Claim>();

        if (Guid.TryParse(result.UserId, out var userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        }

        claims.Add(new Claim(ClaimTypes.Email, email));
        claims.Add(new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? email : fullName));
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, fullName));
        }

        if (result.Roles is not null)
        {
            foreach (var role in result.Roles.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static AuthenticationProperties BuildAuthProperties(AuthResultDto result)
    {
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = AccessTokenName, Value = result.AccessToken },
            new AuthenticationToken { Name = RefreshTokenName, Value = result.RefreshToken },
            new AuthenticationToken { Name = TokenTypeName, Value = string.IsNullOrWhiteSpace(result.TokenType) ? "Bearer" : result.TokenType },
            new AuthenticationToken { Name = ExpiresAtName, Value = result.ExpiresAtUtc?.ToString("O") }
        });

        return properties;
    }

    public static async Task SignInAsync(HttpContext httpContext, AuthResultDto result, string email, string fullName)
    {
        var principal = BuildPrincipal(result, email, fullName);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, BuildAuthProperties(result));
    }

    public static async Task SignOutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public static string? GetRefreshToken(AuthenticationProperties? properties)
    {
        return properties?.GetTokenValue(RefreshTokenName);
    }

    public static string? GetAccessToken(AuthenticationProperties? properties)
    {
        return properties?.GetTokenValue(AccessTokenName);
    }

    public static string? GetTokenType(AuthenticationProperties? properties)
    {
        return properties?.GetTokenValue(TokenTypeName);
    }

    public static DateTimeOffset? GetExpiresAtUtc(AuthenticationProperties? properties)
    {
        var raw = properties?.GetTokenValue(ExpiresAtName);
        return DateTimeOffset.TryParse(raw, out var expiresAt) ? expiresAt : null;
    }
}
