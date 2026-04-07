using Microsoft.Extensions.Logging;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class TourRoutePlaybackService : ITourRoutePlaybackService, IDisposable
{
    private static readonly TimeSpan AudioCooldown = TimeSpan.FromSeconds(20);
    private const double DefaultActivationRadiusMeters = 120;

    private readonly ILocationTrackerService _locationTrackerService;
    private readonly IAudioService _audioService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TourRoutePlaybackService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private TourRouteDto? _currentRoute;
    private int _currentIndex = -1;
    private DateTimeOffset? _lastAutoSelectAtUtc;
    private int? _lastPlayedPoiId;
    private bool _isStarted;

    public event EventHandler<TourRoutePlaybackChangedEventArgs>? ActiveWaypointChanged;

    public TourRouteDto? CurrentRoute => _currentRoute;
    public TourRouteWaypointDto? CurrentWaypoint => IsValidIndex(_currentIndex) ? _currentRoute!.Waypoints[_currentIndex] : null;

    public TourRoutePlaybackService(
        ILocationTrackerService locationTrackerService,
        IAudioService audioService,
        TimeProvider timeProvider,
        ILogger<TourRoutePlaybackService> logger)
    {
        _locationTrackerService = locationTrackerService;
        _audioService = audioService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(TourRouteDto route, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _currentRoute = route;
            _currentIndex = -1;
            _lastAutoSelectAtUtc = null;
            _lastPlayedPoiId = null;

            if (!_isStarted)
            {
                _locationTrackerService.LocationChanged += OnLocationChanged;
                _isStarted = true;
                await _locationTrackerService.StartAsync(cancellationToken);
            }

            _logger.LogInformation("Tour route playback started for route {RouteId} with {WaypointCount} waypoints.", route.Id, route.Waypoints.Count);
            EvaluateLocation(_locationTrackerService.CurrentLocation, isAuto: false, forceInitialSelection: true);
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

            _locationTrackerService.LocationChanged -= OnLocationChanged;
            _isStarted = false;
            _currentRoute = null;
            _currentIndex = -1;
            _lastAutoSelectAtUtc = null;
            _lastPlayedPoiId = null;
            await _locationTrackerService.StopAsync(cancellationToken);
            _logger.LogInformation("Tour route playback stopped.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SelectWaypointAsync(int poiId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_currentRoute is null)
            {
                return;
            }

            var index = _currentRoute.Waypoints.ToList().FindIndex(x => x.Poi.Id == poiId);
            if (index < 0)
            {
                return;
            }

            SetActiveIndex(index, isAuto: false, forceAudio: true, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void OnLocationChanged(object? sender, LocationSample location)
    {
        _ = Task.Run(async () =>
        {
            await _gate.WaitAsync();
            try
            {
                EvaluateLocation(location, isAuto: true, forceInitialSelection: false);
            }
            finally
            {
                _gate.Release();
            }
        });
    }

    private void EvaluateLocation(LocationSample? location, bool isAuto, bool forceInitialSelection)
    {
        if (_currentRoute is null || _currentRoute.Waypoints.Count == 0)
        {
            return;
        }

        if (location is null)
        {
            if (forceInitialSelection && _currentIndex < 0)
            {
                SetActiveIndex(0, isAuto: false, forceAudio: false, cancellationToken: CancellationToken.None);
            }
            return;
        }

        var targetIndex = ResolveTargetIndex(location, forceInitialSelection);
        if (targetIndex < 0)
        {
            return;
        }

        if (targetIndex != _currentIndex)
        {
            SetActiveIndex(targetIndex, isAuto, forceAudio: isAuto, CancellationToken.None, location);
        }
    }

    private int ResolveTargetIndex(LocationSample location, bool forceInitialSelection)
    {
        if (_currentRoute is null)
        {
            return -1;
        }

        var waypoints = _currentRoute.Waypoints;
        if (waypoints.Count == 0)
        {
            return -1;
        }

        if (_currentIndex < 0)
        {
            var nearest = GetNearestWaypointIndex(location);
            if (nearest < 0)
            {
                return -1;
            }

            var nearestDistance = CalculateDistanceMeters(location.Latitude, location.Longitude, waypoints[nearest].Poi.Latitude, waypoints[nearest].Poi.Longitude);
            if (forceInitialSelection || nearestDistance <= GetActivationRadiusMeters(waypoints[nearest]))
            {
                return nearest;
            }

            return -1;
        }

        var currentDistance = CalculateDistanceMeters(location.Latitude, location.Longitude, waypoints[_currentIndex].Poi.Latitude, waypoints[_currentIndex].Poi.Longitude);
        if (currentDistance <= GetActivationRadiusMeters(waypoints[_currentIndex]))
        {
            return _currentIndex;
        }

        var nextIndex = _currentIndex + 1;
        if (nextIndex < waypoints.Count)
        {
            var nextDistance = CalculateDistanceMeters(location.Latitude, location.Longitude, waypoints[nextIndex].Poi.Latitude, waypoints[nextIndex].Poi.Longitude);
            if (nextDistance <= GetActivationRadiusMeters(waypoints[nextIndex]))
            {
                return nextIndex;
            }
        }

        return _currentIndex;
    }

    private int GetNearestWaypointIndex(LocationSample location)
    {
        if (_currentRoute is null)
        {
            return -1;
        }

        var bestIndex = -1;
        var bestDistance = double.MaxValue;

        for (var i = 0; i < _currentRoute.Waypoints.Count; i++)
        {
            var waypoint = _currentRoute.Waypoints[i];
            var distance = CalculateDistanceMeters(location.Latitude, location.Longitude, waypoint.Poi.Latitude, waypoint.Poi.Longitude);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void SetActiveIndex(int index, bool isAuto, bool forceAudio, CancellationToken cancellationToken, LocationSample? location = null)
    {
        if (!IsValidIndex(index) || _currentRoute is null)
        {
            return;
        }

        _currentIndex = index;
        var waypoint = _currentRoute.Waypoints[index];
        var now = _timeProvider.GetUtcNow();
        var shouldPlay = forceAudio || _lastPlayedPoiId != waypoint.Poi.Id || !HasRecentAutoSelection(now);

        if (shouldPlay)
        {
            _lastPlayedPoiId = waypoint.Poi.Id;
            _lastAutoSelectAtUtc = now;
            _ = PlayAudioAsync(waypoint.Poi, cancellationToken);
        }

        ActiveWaypointChanged?.Invoke(this, new TourRoutePlaybackChangedEventArgs(waypoint, isAuto, location));
    }

    private bool HasRecentAutoSelection(DateTimeOffset now)
    {
        return _lastAutoSelectAtUtc.HasValue && now - _lastAutoSelectAtUtc.Value < AudioCooldown;
    }

    private async Task PlayAudioAsync(PoiMobileDto poi, CancellationToken cancellationToken)
    {
        try
        {
            await _audioService.PlayPoiAudioAsync(poi, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-play audio for POI {PoiId}.", poi.Id);
        }
    }

    private static double GetActivationRadiusMeters(TourRouteWaypointDto waypoint)
    {
        return waypoint.Poi.GeofenceRadiusMeters > 0 ? waypoint.Poi.GeofenceRadiusMeters : DefaultActivationRadiusMeters;
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        static double ToRadians(double value) => value * Math.PI / 180d;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private bool IsValidIndex(int index)
    {
        return _currentRoute is not null && index >= 0 && index < _currentRoute.Waypoints.Count;
    }

    public void Dispose()
    {
        _locationTrackerService.LocationChanged -= OnLocationChanged;
        _gate.Dispose();
    }
}
