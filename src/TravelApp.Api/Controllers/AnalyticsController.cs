using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Abstractions.Analytics;
using TravelApp.Application.Dtos.Analytics;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsTrackingService _trackingService;
    private readonly IAnalyticsDashboardService _dashboardService;

    public AnalyticsController(IAnalyticsTrackingService trackingService, IAnalyticsDashboardService dashboardService)
    {
        _trackingService = trackingService;
        _dashboardService = dashboardService;
    }

    [HttpPost("events")]
    public async Task<IActionResult> Track([FromBody] AnalyticsEventRecordDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            request.UserId = User.FindFirst("sub")?.Value
                             ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        }

        await _trackingService.TrackAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetDashboard([FromQuery] AnalyticsDashboardQueryDto query, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardAsync(query, cancellationToken);
        return Ok(result);
    }
}
