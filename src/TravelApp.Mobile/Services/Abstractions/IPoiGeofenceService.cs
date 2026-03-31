using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface IPoiGeofenceService
{
    event Action<PoiMobileDto>? OnPoiEntered;

    void SetPois(IEnumerable<PoiMobileDto> pois);
    void UpdateLocation(LocationSample locationSample);
}
