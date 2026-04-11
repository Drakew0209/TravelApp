namespace TravelApp.Admin.Web.Models;

public sealed class AdminDashboardViewModel
{
    public int TourCount { get; set; }
    public int PublishedTourCount { get; set; }
    public int DraftTourCount { get; set; }
    public int PoiCount { get; set; }
    public int UserCount { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public IReadOnlyList<DashboardTourSummary> RecentTours { get; set; } = [];
    public IReadOnlyList<DashboardPoiSummary> RecentPois { get; set; } = [];
}

public sealed class DashboardTourSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int PoiCount { get; set; }
}

public sealed class DashboardPoiSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsUsedInTour { get; set; }
}
