using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Abstractions.Pois;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/admin/pois")]
[Authorize(Roles = "Owner,Admin,SuperAdmin")]
public class AdminPoisController : ControllerBase
{
    private readonly IPoiQueryService _poiQueryService;

    public AdminPoisController(IPoiQueryService poiQueryService)
    {
        _poiQueryService = poiQueryService;
    }

    [HttpPost("backfill-speech-texts")]
    public async Task<IActionResult> BackfillSpeechTexts(CancellationToken cancellationToken)
    {
        var updatedCount = await _poiQueryService.BackfillSpeechTextsAsync(cancellationToken);
        return Ok(new { updatedCount });
    }
}
