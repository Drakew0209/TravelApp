using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using TravelApp.Admin.Web.Models;
using TravelApp.Admin.Web.Services;
using TravelApp.Application.Dtos.Analytics;
using TravelApp.Application.Utilities;

namespace TravelApp.Admin.Web.Controllers;

[Authorize(Roles = "Owner,Admin,SuperAdmin")]
public class AdminController : Controller
{
    private readonly ITravelAppApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public AdminController(ITravelAppApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(string range = "30d", string granularity = "day", CancellationToken cancellationToken = default)
    {
        var cultureCode = NormalizeLanguageCode(CultureInfo.CurrentUICulture.Name);
        var pois = await _apiClient.GetPoisAsync(cultureCode, cancellationToken);
        var tours = await _apiClient.GetToursAsync(cancellationToken);
        var users = await _apiClient.GetUsersAsync(cancellationToken);
        var analyticsQuery = BuildAnalyticsQuery(range, granularity);
        var analytics = await _apiClient.GetAnalyticsDashboardAsync(analyticsQuery, cancellationToken);
        var vm = new AdminDashboardViewModel
        {
            Range = range,
            Granularity = granularity,
            PoiCount = pois.Count,
            QrCount = analytics.QrScans,
            TourCount = tours.Count,
            PublishedTourCount = tours.Count(x => x.IsPublished),
            DraftTourCount = tours.Count(x => !x.IsPublished),
            UserCount = users.Count,
            UniqueUsers = analytics.UniqueUsers,
            UniqueGuests = analytics.UniqueGuests,
            ApiBaseUrl = _configuration["TravelAppApi:BaseUrl"] ?? string.Empty,
            Analytics = analytics,
            RecentTours = tours.OrderByDescending(x => x.Id).Take(5).Select(x => new DashboardTourSummary
            {
                Id = x.Id,
                Name = x.Name,
                IsPublished = x.IsPublished,
                PoiCount = x.Pois.Count
            }).ToList(),
            RecentPois = pois.OrderByDescending(x => x.Id).Take(5).Select(x => new DashboardPoiSummary
            {
                Id = x.Id,
                Title = x.Title,
                Category = x.Category,
                IsUsedInTour = x.IsUsedInTour
            }).ToList()
        };

        return View(vm);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
        return string.IsNullOrWhiteSpace(normalized) ? "vi-VN" : normalized;
    }

    private static AnalyticsDashboardQueryDto BuildAnalyticsQuery(string range, string granularity)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedRange = string.IsNullOrWhiteSpace(range) ? "30d" : range.Trim().ToLowerInvariant();
        var fromUtc = normalizedRange switch
        {
            "7d" => now.AddDays(-7),
            "30d" => now.AddDays(-30),
            "90d" => now.AddDays(-90),
            "365d" => now.AddDays(-365),
            _ => now.AddDays(-30)
        };

        return new AnalyticsDashboardQueryDto
        {
            FromUtc = fromUtc,
            ToUtc = now,
            Granularity = Enum.TryParse<AnalyticsGranularityDto>(granularity, true, out var parsed)
                ? parsed
                : AnalyticsGranularityDto.Day,
            RecentLimit = 25
        };
    }
}
