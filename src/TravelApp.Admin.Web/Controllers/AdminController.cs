using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Admin.Web.Models;
using TravelApp.Admin.Web.Services;

namespace TravelApp.Admin.Web.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ITravelAppApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public AdminController(ITravelAppApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var pois = await _apiClient.GetPoisAsync("vi", cancellationToken);
        var tours = await _apiClient.GetToursAsync(cancellationToken);
        var users = await _apiClient.GetUsersAsync(cancellationToken);
        var vm = new AdminDashboardViewModel
        {
            PoiCount = pois.Count,
            TourCount = tours.Count,
            PublishedTourCount = tours.Count(x => x.IsPublished),
            DraftTourCount = tours.Count(x => !x.IsPublished),
            UserCount = users.Count,
            ApiBaseUrl = _configuration["TravelAppApi:BaseUrl"] ?? string.Empty,
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
}
