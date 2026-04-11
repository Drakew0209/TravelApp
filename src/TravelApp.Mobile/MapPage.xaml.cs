using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelApp.Models;
using TravelApp.Models.Runtime;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private Microsoft.Maui.Controls.Maps.Map? _map;
    private int? _selectedPoiId;
    private Location? _userLocation;

    public MapPage()
    {
        InitializeComponent();

        _viewModel = MauiProgram.Services.GetRequiredService<MapViewModel>();
        BindingContext = _viewModel;
        _viewModel.PoiPins.CollectionChanged += OnPoiPinsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _map = map;
            if (_map is null)
            {
                return;
            }

            // Initialize map
            _map.IsShowingUser = true;

            // Load data and location
            await _viewModel.InitializeAsync();

            _userLocation = _viewModel.UserLocation is null
                ? null
                : new Location(_viewModel.UserLocation.Latitude, _viewModel.UserLocation.Longitude);

            // Add pins to map
            AddPinsToMap();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load map: {ex.Message}", "OK");
        }
    }

    private void AddPinsToMap()
    {
        if (_map is null)
        {
            return;
        }

        _map.Pins.Clear();

        var points = new List<Location>();

        // Add user location as a special marker
        if (_viewModel.UserLocation is not null)
        {
            var userLocation = new Location(_viewModel.UserLocation.Latitude, _viewModel.UserLocation.Longitude);
            var userPin = new Pin
            {
                Label = "📍 Vị trí của bạn",
                Address = "Your current location",
                Location = userLocation,
                Type = PinType.Place,
            };
            _map.Pins.Add(userPin);
            points.Add(userLocation);
        }

        // Add POI pins
        foreach (var poi in _viewModel.PoiPins)
        {
            var pin = new Pin
            {
                Label = _selectedPoiId == poi.PoiId ? $"★ {poi.Title}" : poi.Title,
                Address = poi.Address,
                Location = new Location(poi.Latitude, poi.Longitude),
                Type = PinType.SavedPin,
            };

            points.Add(pin.Location);

            pin.MarkerClicked += async (_, args) =>
            {
                _selectedPoiId = poi.PoiId;
                AddPinsToMap();
                args.HideInfoWindow = true;

                await AnimateToPoiAsync(poi.Latitude, poi.Longitude);
                await Task.Delay(140);

                if (_viewModel.OpenPoiDetailCommand is Command<MapPinItem> command)
                {
                    command.Execute(poi);
                }
            };

            _map.Pins.Add(pin);
        }

        // Move map to show all pins with a luxury fit-to-bounds view
        if (points.Count > 0)
        {
            var minLat = points.Min(x => x.Latitude);
            var maxLat = points.Max(x => x.Latitude);
            var minLng = points.Min(x => x.Longitude);
            var maxLng = points.Max(x => x.Longitude);

            var center = new Location((minLat + maxLat) / 2d, (minLng + maxLng) / 2d);
            var latSpanKm = Math.Max(0.8, (maxLat - minLat) * 111.32 * 1.35);
            var lngSpanKm = Math.Max(0.8, (maxLng - minLng) * 111.32 * 1.35);
            var radiusKm = Math.Max(latSpanKm, lngSpanKm) / 2d;

            _map.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(Math.Min(10d, Math.Max(1.1d, radiusKm)))));
        }
    }

    private Task AnimateToPoiAsync(double latitude, double longitude)
    {
        if (_map is null)
        {
            return Task.CompletedTask;
        }

        var focus = new Location(latitude, longitude);
        _map.MoveToRegion(MapSpan.FromCenterAndRadius(focus, Distance.FromMeters(420)));
        return Task.CompletedTask;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PoiPins.CollectionChanged -= OnPoiPinsChanged;
        _viewModel.Dispose();
    }

    private void OnPoiPinsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(AddPinsToMap);
    }
}
