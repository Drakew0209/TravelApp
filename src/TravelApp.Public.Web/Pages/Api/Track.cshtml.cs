using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelApp.Application.Dtos.Analytics;
using TravelApp.Public.Web.Services;

namespace TravelApp.Public.Web.Pages.Api;

[IgnoreAntiforgeryToken]
public sealed class TrackModel : PageModel
{
    private readonly ITravelAppPublicApiClient _apiClient;

    public TrackModel(ITravelAppPublicApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = await Request.ReadFromJsonAsync<AnalyticsEventRecordDto>(cancellationToken);
            if (request is null)
            {
                return BadRequest();
            }

            await _apiClient.TrackEventAsync(request, cancellationToken);
            return new AcceptedResult();
        }
        catch (OperationCanceledException)
        {
            return new AcceptedResult();
        }
    }
}
