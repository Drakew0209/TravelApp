using TravelApp.Application.Dtos.Analytics;

namespace TravelApp.Application.Abstractions.Analytics;

public interface IAnalyticsDashboardService
{
    Task<AnalyticsDashboardDto> GetDashboardAsync(AnalyticsDashboardQueryDto request, CancellationToken cancellationToken = default);
}
