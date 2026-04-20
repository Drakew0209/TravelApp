using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelApp.Public.Web.Services;

namespace TravelApp.Public.Web.Pages.Api;

[IgnoreAntiforgeryToken]
public sealed class HistoryModel : PageModel
{
    private readonly PublicLibraryApiProxyService _libraryApiProxyService;

    public HistoryModel(PublicLibraryApiProxyService libraryApiProxyService)
    {
        _libraryApiProxyService = libraryApiProxyService;
    }

    public async Task<IActionResult> OnGetAsync(string? lang, CancellationToken cancellationToken)
    {
        var items = await _libraryApiProxyService.GetHistoryAsync(lang, cancellationToken);
        return items is null ? Unauthorized() : new JsonResult(items);
    }

    public async Task<IActionResult> OnPostAsync(int poiId, CancellationToken cancellationToken)
    {
        return await _libraryApiProxyService.AddHistoryAsync(poiId, cancellationToken) ? new NoContentResult() : Unauthorized();
    }

    public async Task<IActionResult> OnDeleteAsync(int poiId, CancellationToken cancellationToken)
    {
        return await _libraryApiProxyService.RemoveHistoryAsync(poiId, cancellationToken) ? new NoContentResult() : Unauthorized();
    }

    public async Task<IActionResult> OnDeleteAllAsync(CancellationToken cancellationToken)
    {
        return await _libraryApiProxyService.ClearHistoryAsync(cancellationToken) ? new NoContentResult() : Unauthorized();
    }
}
