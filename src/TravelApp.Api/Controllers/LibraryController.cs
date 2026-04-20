using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Abstractions.Library;
using TravelApp.Application.Dtos.Library;

namespace TravelApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/library")]
public sealed class LibraryController : ControllerBase
{
    private readonly IUserLibraryService _libraryService;

    public LibraryController(IUserLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    [HttpGet("bookmarks")]
    public async Task<ActionResult<IReadOnlyList<BookmarkStateDto>>> GetBookmarksAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _libraryService.GetBookmarksAsync(userId.Value, cancellationToken));
    }

    [HttpPost("bookmarks/{poiId:int}")]
    public async Task<IActionResult> ToggleBookmarkAsync(int poiId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _libraryService.ToggleBookmarkAsync(userId.Value, poiId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("bookmarks/{poiId:int}")]
    public async Task<IActionResult> RemoveBookmarkAsync(int poiId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _libraryService.RemoveBookmarkAsync(userId.Value, poiId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("bookmarks")]
    public async Task<IActionResult> ClearBookmarksAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _libraryService.ClearBookmarksAsync(userId.Value, cancellationToken);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<HistoryStateDto>>> GetHistoryAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _libraryService.GetHistoryAsync(userId.Value, cancellationToken));
    }

    [HttpPost("history/{poiId:int}")]
    public async Task<IActionResult> AddHistoryAsync(int poiId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _libraryService.AddHistoryAsync(userId.Value, poiId, null, cancellationToken);
        return NoContent();
    }

    [HttpDelete("history/{poiId:int}")]
    public async Task<IActionResult> RemoveHistoryAsync(int poiId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _libraryService.RemoveHistoryAsync(userId.Value, poiId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("history")]
    public async Task<IActionResult> ClearHistoryAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _libraryService.ClearHistoryAsync(userId.Value, cancellationToken);
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userId, out var id) ? id : null;
    }
}
