using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TravelApp.Application.Abstractions.Auth;
using System.Security.Claims;

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

        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (result is null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(result);
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

        return Ok(result);
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
}

public record LoginRequestDto(string Email, string Password);
public record RefreshTokenRequestDto(string RefreshToken);
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
