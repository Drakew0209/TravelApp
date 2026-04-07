using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using TravelApp.Models;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class MapViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IPoiApiClient _poiApiClient;
    private readonly ILocationProvider _locationProvider;
    private readonly ILogService _logService;
    
    private string _statusText = "Đang tải vị trí...";
    private LocationSample? _userLocation;
    private bool _isLoading = true;

    public ObservableCollection<MapPinItem> PoiPins { get; } = [];
    public ObservableCollection<PoiModel> PoisData { get; } = [];

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public LocationSample? UserLocation
    {
        get => _userLocation;
        private set
        {
            if (_userLocation == value) return;
            _userLocation = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenPoiDetailCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MapViewModel(
        IPoiApiClient poiApiClient,
        ILocationProvider locationProvider,
        ILogService logService)
    {
        _poiApiClient = poiApiClient;
        _locationProvider = locationProvider;
        _logService = logService;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        RefreshCommand = new Command(async () => await LoadDataAsync());
        OpenPoiDetailCommand = new Command<MapPinItem>(async pin =>
        {
            if (pin is null) return;
            
            StatusText = $"Mở chi tiết cho: {pin.Title}";
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={pin.PoiId}");
        });
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;
            StatusText = "Đang lấy vị trí hiện tại...";

            // Get user location first
            UserLocation = await _locationProvider.GetCurrentLocationAsync(cancellationToken);
            
            if (UserLocation is null)
            {
                StatusText = "Không thể lấy vị trí GPS. Hiển thị tất cả POI.";
            }
            else
            {
                StatusText = $"Vị trí: {UserLocation.Latitude:F4}, {UserLocation.Longitude:F4}";
            }

            // Load POI data
            await LoadDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Log(nameof(MapViewModel), $"InitializeAsync error: {ex.Message}");
            StatusText = "Lỗi tải dữ liệu. Vui lòng thử lại.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch POIs from API
            var pois = await _poiApiClient.GetAllAsync(languageCode: "vi", cancellationToken: cancellationToken);
            
            if (pois is null || pois.Count == 0)
            {
                StatusText = "Không có POI nào để hiển thị.";
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PoisData.Clear();
                PoiPins.Clear();

                foreach (var poi in pois)
                {
                    // Add to data collection
                    PoisData.Add(new PoiModel
                    {
                        Id = poi.Id,
                        Title = poi.Title,
                        Subtitle = poi.Subtitle,
                        ImageUrl = poi.ImageUrl,
                        Location = poi.Location,
                        Distance = CalculateDistance(poi),
                        Duration = poi.Duration ?? "30 min",
                        Description = poi.Description,
                        Provider = poi.Provider,
                        Credit = poi.Credit
                    });

                    // Parse location to get coordinates (dummy coordinates for now)
                    var (lat, lng) = ParseLocationCoordinates(poi);
                    
                    // Add to map pins
                    PoiPins.Add(new MapPinItem
                    {
                        PoiId = poi.Id,
                        Title = poi.Title,
                        Address = poi.Location,
                        Latitude = lat,
                        Longitude = lng
                    });
                }

                StatusText = $"Đã tải {pois.Count} POI";
            });
        }
        catch (Exception ex)
        {
            _logService.Log(nameof(MapViewModel), $"LoadDataAsync error: {ex.Message}");
            StatusText = "Lỗi khi tải POI.";
        }
    }

    private string CalculateDistance(Models.Contracts.PoiDto poi)
    {
        // Placeholder - in real implementation, calculate from user location
        return "< 5 km";
    }

    private (double lat, double lng) ParseLocationCoordinates(Models.Contracts.PoiDto poi)
    {
        // Default HCM + Hanoi area coordinates based on location name
        if (poi.Location.Contains("HCM") || poi.Location.Contains("TPHCM") || poi.Location.Contains("Sài Gòn"))
        {
            // HCM coordinates (random within city bounds)
            var random = new Random();
            return (10.7 + (random.NextDouble() - 0.5) * 0.1, 106.7 + (random.NextDouble() - 0.5) * 0.1);
        }
        else if (poi.Location.Contains("Hanoi") || poi.Location.Contains("Hà Nội"))
        {
            // Hanoi coordinates (random within city bounds)
            var random = new Random();
            return (21.0 + (random.NextDouble() - 0.5) * 0.1, 105.8 + (random.NextDouble() - 0.5) * 0.1);
        }
        else
        {
            // Default to HCM center
            return (10.762622, 106.660172);
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
