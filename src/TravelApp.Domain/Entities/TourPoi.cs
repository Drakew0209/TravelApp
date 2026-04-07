namespace TravelApp.Domain.Entities;

public class TourPoi
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public int PoiId { get; set; }
    public int SortOrder { get; set; }
    public double? DistanceFromPreviousMeters { get; set; }

    public Tour Tour { get; set; } = null!;
    public Poi Poi { get; set; } = null!;
}
