using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelApp.Models.Runtime;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private Microsoft.Maui.Controls.Maps.Map? _map;

    public MapPage()
    {
        InitializeComponent();

        _viewModel = MauiProgram.Services.GetRequiredService<MapViewModel>();
        BindingContext = _viewModel;
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

        // Add user location as a special marker
        if (_viewModel.UserLocation is not null)
        {
            var userPin = new Pin
            {
                Label = "📍 Vị trí của bạn",
                Address = "Your current location",
                Location = new Location(_viewModel.UserLocation.Latitude, _viewModel.UserLocation.Longitude),
            };
            _map.Pins.Add(userPin);
        }

        // Add POI pins
        foreach (var poi in _viewModel.PoiPins)
        {
            var pin = new Pin
            {
                Label = poi.Title,
                Address = poi.Address,
                Location = new Location(poi.Latitude, poi.Longitude),
            };

            // Handle pin click
            pin.InfoWindowClicked += async (s, e) =>
            {
                if (_viewModel.OpenPoiDetailCommand is Command command)
                {
                    command.Execute(poi);
                }
            };

            _map.Pins.Add(pin);
        }

        // Move map to show all pins
        if (_viewModel.PoiPins.Count > 0)
        {
            var firstPin = _viewModel.PoiPins.First();
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(firstPin.Latitude, firstPin.Longitude),
                Distance.FromKilometers(5)));
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Dispose();
    }
}
