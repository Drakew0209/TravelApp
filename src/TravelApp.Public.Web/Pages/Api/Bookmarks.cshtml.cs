using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelApp.Public.Web.Services;

namespace TravelApp.Public.Web.Pages.Api;

[IgnoreAntiforgeryToken]
public sealed class BookmarksModel : PageModel
{
    private readonly PublicLibraryApiProxyService _libraryApiProxyService;

    public BookmarksModel(PublicLibraryApiProxyService libraryApiProxyService)
    {
        _libraryApiProxyService = libraryApiProxyService;
    }

    public async Task<IActionResult> OnGetAsync(string? lang, CancellationToken cancellationToken)
    {
        var items = await _libraryApiProxyService.GetBookmarksAsync(lang, cancellationToken);
        return items is null ? Unauthorized() : new JsonResult(items);
    }

    public async Task<IActionResult> OnPostAsync(int poiId, CancellationToken cancellationToken)
    {
        return await _libraryApiProxyService.ToggleBookmarkAsync(poiId, cancellationToken) ? new NoContentResult() : Unauthorized();
    }

    public async Task<IActionResult> OnDeleteAsync(int poiId, CancellationToken cancellationToken)
    {
        return await _libraryApiProxyService.RemoveBookmarkAsync(poiId, cancellationToken) ? new NoContentResult() : Unauthorized();
    }

    public async Task<IActionResult> OnDeleteAllAsync(CancellationToken cancellationToken)
    {
        return await _libraryApiProxyService.ClearBookmarksAsync(cancellationToken) ? new NoContentResult() : Unauthorized();
    }
}
