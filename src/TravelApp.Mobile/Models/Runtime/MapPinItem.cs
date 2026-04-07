namespace TravelApp.Models.Runtime;

/// <summary>
/// Represents a POI marker pin on the map
/// </summary>
public class MapPinItem
{
    public int PoiId { get; set; }
    public required string Title { get; set; }
    public required string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
