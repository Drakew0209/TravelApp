using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Dtos.Tours;
using TravelApp.Application.Abstractions.Tours;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/tours")]
public class ToursController : ControllerBase
{
    private readonly ITourQueryService _tourQueryService;

    public ToursController(ITourQueryService tourQueryService)
    {
        _tourQueryService = tourQueryService;
    }

    [HttpGet("{anchorPoiId:int}")]
    public async Task<IActionResult> GetByAnchorPoiId(int anchorPoiId, [FromQuery(Name = "lang")] string? languageCode, CancellationToken cancellationToken)
    {
        var result = await _tourQueryService.GetByAnchorPoiIdAsync(anchorPoiId, languageCode, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TourRouteDto>>> GetAll([FromQuery(Name = "lang")] string? languageCode, CancellationToken cancellationToken)
    {
        var result = await _tourQueryService.GetAllPublishedAsync(languageCode, cancellationToken);
        return Ok(result);
    }
}
