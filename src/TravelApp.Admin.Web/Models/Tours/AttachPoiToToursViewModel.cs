using System.ComponentModel.DataAnnotations;

namespace TravelApp.Admin.Web.Models.Tours;

public sealed class AttachPoiToToursViewModel
{
    [Required]
    public int PoiId { get; set; }

    public string PoiTitle { get; set; } = string.Empty;
    public string? PoiSubtitle { get; set; }
    public string? PoiLocation { get; set; }

    [Required]
    public List<int> SelectedTourIds { get; set; } = [];

    public List<AttachPoiTourItemViewModel> AvailableTours { get; set; } = [];
}

public sealed class AttachPoiTourItemViewModel
{
    public int TourId { get; set; }
    public string TourName { get; set; } = string.Empty;
    public string? TourDescription { get; set; }
    public bool IsPublished { get; set; }
    public int PoiCount { get; set; }
    public bool IsSelected { get; set; }
}
