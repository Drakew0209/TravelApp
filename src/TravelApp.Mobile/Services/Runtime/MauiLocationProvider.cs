using Microsoft.Maui.Devices.Sensors;
using Microsoft.Extensions.Logging;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class MauiLocationProvider : ILocationProvider
{
    private readonly ILogger<MauiLocationProvider> _logger;

    public MauiLocationProvider(ILogger<MauiLocationProvider> logger)
    {
        _logger = logger;
    }

    public async Task<LocationSample?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (permissionStatus != PermissionStatus.Granted)
            {
                _logger.LogDebug("GPS: location permission was not granted.");
                return null;
            }

            var bestRequest = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
            var location = await Geolocation.Default.GetLocationAsync(bestRequest, cancellationToken);

            if (location is null)
            {
                var retryRequest = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                location = await Geolocation.Default.GetLocationAsync(retryRequest, cancellationToken);
            }

            if (location is null)
            {
                var lastKnown = await Geolocation.Default.GetLastKnownLocationAsync();
                if (lastKnown is not null)
                {
                    return new LocationSample(lastKnown.Latitude, lastKnown.Longitude, DateTimeOffset.UtcNow);
                }
            }

            if (location is null)
            {
                _logger.LogDebug("GPS: no location available from platform provider. Make sure OS location services are enabled.");
                return null;
            }

            return new LocationSample(location.Latitude, location.Longitude, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GPS: failed to fetch location sample.");
            return null;
        }
    }
}
