using Microsoft.Extensions.Logging;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class TourMapRouteService : ITourMapRouteService
{
    private const double RouteRadiusMeters = 2000;
    private const double RefreshDistanceThresholdMeters = 30;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(8);

    private readonly ILocationPollingService _locationPollingService;
    private readonly IPoiApiService _poiApiService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TourMapRouteService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _isStarted;
    private string _languageCode = "en";
    private DateTimeOffset _lastRefreshUtc = DateTimeOffset.MinValue;
    private LocationSample? _lastRouteLocation;

    public event EventHandler<TourMapRouteUpdatedEventArgs>? RouteUpdated;

    public TourMapRouteSnapshot? CurrentSnapshot { get; private set; }

    public TourMapRouteService(
        ILocationPollingService locationPollingService,
        IPoiApiService poiApiService,
        TimeProvider timeProvider,
        ILogger<TourMapRouteService> logger)
    {
        _locationPollingService = locationPollingService;
        _poiApiService = poiApiService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_isStarted)
            {
                return;
            }

            _languageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
            _locationPollingService.OnLocationUpdated += OnLocationUpdated;
            _isStarted = true;

            await _locationPollingService.StartAsync(cancellationToken);

            if (_locationPollingService.CurrentLocation is not null)
            {
                _ = RefreshRouteAsync(_locationPollingService.CurrentLocation, cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_isStarted)
            {
                return;
            }

            _locationPollingService.OnLocationUpdated -= OnLocationUpdated;
            _isStarted = false;
            await _locationPollingService.StopAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void OnLocationUpdated(LocationSample location)
    {
        _ = RefreshRouteAsync(location, CancellationToken.None);
    }

    private async Task RefreshRouteAsync(LocationSample location, CancellationToken cancellationToken)
    {
        if (!ShouldRefresh(location))
        {
            return;
        }

        try
        {
            var pois = await _poiApiService.GetPoisAsync(
                location.Latitude,
                location.Longitude,
                RouteRadiusMeters,
                _languageCode,
                pageNumber: 1,
                pageSize: 30,
                cancellationToken: cancellationToken);

            var waypoints = pois
                .OrderBy(x => x.DistanceMeters ?? double.MaxValue)
                .Take(12)
                .Select(x => new TourMapWaypoint
                {
                    PoiId = x.Id,
                    Title = x.Title,
                    Location = x.Location,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    DistanceMeters = x.DistanceMeters
                })
                .ToList();

            CurrentSnapshot = new TourMapRouteSnapshot
            {
                UpdatedAtUtc = _timeProvider.GetUtcNow(),
                CurrentLocation = location,
                Waypoints = waypoints
            };

            _lastRefreshUtc = CurrentSnapshot.UpdatedAtUtc;
            _lastRouteLocation = location;

            RouteUpdated?.Invoke(this, new TourMapRouteUpdatedEventArgs(CurrentSnapshot));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TourMapRoute: failed to refresh route.");
        }
    }

    private bool ShouldRefresh(LocationSample location)
    {
        var now = _timeProvider.GetUtcNow();

        if (_lastRefreshUtc != DateTimeOffset.MinValue && now - _lastRefreshUtc < RefreshInterval)
        {
            return false;
        }

        if (_lastRouteLocation is null)
        {
            return true;
        }

        var distance = CalculateDistanceMeters(
            _lastRouteLocation.Latitude,
            _lastRouteLocation.Longitude,
            location.Latitude,
            location.Longitude);

        return distance >= RefreshDistanceThresholdMeters;
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000;
        static double ToRadians(double value) => value * Math.PI / 180d;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }
}
