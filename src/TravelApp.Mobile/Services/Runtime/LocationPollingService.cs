using Microsoft.Extensions.Logging;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class LocationPollingService : ILocationPollingService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const double MinDistanceForUpdateMeters = 5;

    private readonly ILocationProvider _locationProvider;
    private readonly ILogService _logService;
    private readonly ILogger<LocationPollingService> _logger;

    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    public event Action<LocationSample>? OnLocationUpdated;

    public LocationSample? CurrentLocation { get; private set; }

    public LocationPollingService(
        ILocationProvider locationProvider,
        ILogService logService,
        ILogger<LocationPollingService> logger)
    {
        _locationProvider = locationProvider;
        _logService = logService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_pollingTask is { IsCompleted: false })
        {
            _logger.LogDebug("Location polling: start ignored because already running.");
            return Task.CompletedTask;
        }

        _pollingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollingTask = PollLoopAsync(_pollingCts.Token);

        _logger.LogInformation("Location polling: started, interval={IntervalSeconds}s.", PollInterval.TotalSeconds);
        _logService.Log("GPS", $"Polling started (interval={PollInterval.TotalSeconds:0}s)");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_pollingCts is null || _pollingTask is null)
        {
            return;
        }

        _pollingCts.Cancel();

        try
        {
            await _pollingTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _pollingCts.Dispose();
            _pollingCts = null;
            _pollingTask = null;
            _logger.LogInformation("Location polling: stopped.");
            _logService.Log("GPS", "Polling stopped");
        }
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!cancellationToken.IsCancellationRequested)
        {
            var sample = await _locationProvider.GetCurrentLocationAsync(cancellationToken);
            if (sample is not null)
            {
                if (CurrentLocation is not null)
                {
                    var distance = CalculateDistanceMeters(
                        CurrentLocation.Latitude,
                        CurrentLocation.Longitude,
                        sample.Latitude,
                        sample.Longitude);

                    if (distance < MinDistanceForUpdateMeters)
                    {
                        _logger.LogDebug(
                            "GPS update skipped: movement {DistanceMeters:F1}m < threshold {ThresholdMeters:F1}m.",
                            distance,
                            MinDistanceForUpdateMeters);
                        _logService.Log("GPS", $"Skip movement={distance:F1}m < {MinDistanceForUpdateMeters:F1}m");
                        goto wait_next_tick;
                    }
                }

                CurrentLocation = sample;
                _logger.LogInformation("GPS update: lat={Latitude:F6}, lng={Longitude:F6}", sample.Latitude, sample.Longitude);
                _logService.Log("GPS", $"Update lat={sample.Latitude:F6}, lng={sample.Longitude:F6}");
                OnLocationUpdated?.Invoke(sample);
            }

wait_next_tick:
            if (!await timer.WaitForNextTickAsync(cancellationToken))
            {
                break;
            }
        }
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        static double ToRadians(double value) => value * Math.PI / 180;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }
}
