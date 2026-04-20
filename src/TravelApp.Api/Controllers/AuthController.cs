using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TravelApp.Application.Abstractions.Auth;
using System.Security.Claims;
using TravelApp.Application.Dtos.Auth;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <remarks>
    /// Demo credentials:
    /// - Email: demo@example.com, Password: Demo@123456
    /// - Email: khanh@example.com, Password: Khanh@123456
    /// - Email: guest@example.com, Password: Guest@123456
    /// </remarks>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> LoginAsync([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request.Email.Trim(), request.Password);

        if (result is null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(ToApiResult(result));
    }

    /// <summary>
    /// Register a new account with the default User role.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> RegisterAsync([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authService.RegisterAsync(request);
            if (result is null)
            {
                return BadRequest(new { message = "Unable to register user." });
            }

            return Ok(ToApiResult(result));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync([FromBody] RefreshTokenRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
        return NoContent();
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> RefreshTokenAsync([FromBody] RefreshTokenRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (result is null)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        return Ok(ToApiResult(result));
    }

    /// <summary>
    /// Get current user profile (requires authentication)
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
            return Unauthorized();

        var profile = await _authService.GetUserProfileAsync(id);
        if (profile is null)
            return NotFound();

        return Ok(profile);
    }

    private static AuthResultDto ToApiResult(TravelApp.Application.Abstractions.Auth.AuthResultDto result)
    {
        return new AuthResultDto(
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresAtUtc,
            result.TokenType,
            result.UserId,
            result.Roles,
            result.FullName);
    }
}

public record LoginRequestDto(string Email, string Password);
public record RefreshTokenRequestDto(string RefreshToken);
public record AuthResultDto(
    string AccessToken,
    string? RefreshToken = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string TokenType = "Bearer",
    string? UserId = null,
    IReadOnlyList<string>? Roles = null,
    string? FullName = null);
public record UserProfileDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName = "");

