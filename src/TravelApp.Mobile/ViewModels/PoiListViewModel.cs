using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class PoiListViewModel : INotifyPropertyChanged
{
    private const double DefaultRadiusMeters = 1500;

    private readonly IPoiApiService _poiApiService;
    private readonly IPoiGeofenceService _poiGeofenceService;
    private readonly ILocationPollingService _locationPollingService;
    private bool _isSubscribedToLocation;
    private bool _isLoading;
    private string _statusText = "Idle";

    public ObservableCollection<PoiMobileDto> Pois { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadPoisCommand { get; }

    public PoiListViewModel(
        IPoiApiService poiApiService,
        IPoiGeofenceService poiGeofenceService,
        ILocationPollingService locationPollingService)
    {
        _poiApiService = poiApiService;
        _poiGeofenceService = poiGeofenceService;
        _locationPollingService = locationPollingService;
        LoadPoisCommand = new Command(async () => await LoadPoisAsync());
    }

    public async Task StartPollingAsync(CancellationToken cancellationToken = default)
    {
        if (!_isSubscribedToLocation)
        {
            _locationPollingService.OnLocationUpdated += HandleLocationUpdated;
            _isSubscribedToLocation = true;
        }

        await _locationPollingService.StartAsync(cancellationToken);
    }

    public async Task StopPollingAsync(CancellationToken cancellationToken = default)
    {
        if (_isSubscribedToLocation)
        {
            _locationPollingService.OnLocationUpdated -= HandleLocationUpdated;
            _isSubscribedToLocation = false;
        }

        await _locationPollingService.StopAsync(cancellationToken);
        StatusText = "Stopped";
    }

    public async Task LoadPoisAsync(CancellationToken cancellationToken = default)
    {
        var currentLocation = _locationPollingService.CurrentLocation;
        if (currentLocation is null)
        {
            StatusText = "GPS unavailable";
            return;
        }

        await LoadPoisByLocationAsync(currentLocation, cancellationToken);
    }

    private async Task LoadPoisByLocationAsync(LocationSample location, CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            var lang = UserProfileService.PreferredLanguage;
            var pois = await _poiApiService.GetPoisAsync(
                location.Latitude,
                location.Longitude,
                DefaultRadiusMeters,
                lang,
                pageNumber: 1,
                pageSize: 50,
                cancellationToken: cancellationToken);

            _poiGeofenceService.SetPois(pois);
            _poiGeofenceService.UpdateLocation(new LocationSample(location.Latitude, location.Longitude, DateTimeOffset.UtcNow));

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Pois.Clear();
                foreach (var poi in pois)
                {
                    Pois.Add(poi);
                }
            });

            StatusText = $"GPS {location.Latitude:F5}, {location.Longitude:F5} - {pois.Count} POIs";
        }
        catch (Exception ex)
        {
            StatusText = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void HandleLocationUpdated(LocationSample location)
    {
        _ = LoadPoisByLocationAsync(location);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
