using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class TourMapRoutePage : ContentPage
{
    private readonly TourMapRouteViewModel _viewModel;
    private readonly Microsoft.Maui.Controls.Maps.Map _map;

    public TourMapRoutePage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<TourMapRouteViewModel>();
        BindingContext = _viewModel;
        _viewModel.RouteChanged += OnRouteChanged;

        _map = new Microsoft.Maui.Controls.Maps.Map
        {
            MapType = MapType.Street,
            IsTrafficEnabled = false,
            IsShowingUser = false
        };

        TourMapContainer.Children.Add(_map);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.StartAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.StopAsync();
        _viewModel.RouteChanged -= OnRouteChanged;
        _viewModel.Dispose();
    }

    private void OnRouteChanged(object? sender, EventArgs e)
    {
        RenderRoute();
    }

    private void RenderRoute()
    {
        _map.Pins.Clear();
        _map.MapElements.Clear();

        var snapshot = _viewModel.Snapshot;
        if (snapshot?.CurrentLocation is null)
        {
            return;
        }

        var center = new Location(snapshot.CurrentLocation.Latitude, snapshot.CurrentLocation.Longitude);
        var selectedWaypoint = _viewModel.SelectedWaypoint;
        var mapCenter = selectedWaypoint is null
            ? center
            : new Location(selectedWaypoint.Latitude, selectedWaypoint.Longitude);

        _map.MoveToRegion(MapSpan.FromCenterAndRadius(mapCenter, Distance.FromKilometers(2)));

        _map.Pins.Add(new Pin
        {
            Label = "You",
            Address = "Current location",
            Type = PinType.SavedPin,
            Location = center
        });

        if (snapshot.Waypoints.Count == 0)
        {
            return;
        }

        var polyline = new Polyline
        {
            StrokeColor = Color.FromArgb("#E31667"),
            StrokeWidth = 4
        };

        polyline.Geopath.Add(center);

        foreach (var waypoint in snapshot.Waypoints)
        {
            var location = new Location(waypoint.Latitude, waypoint.Longitude);
            polyline.Geopath.Add(location);

            var isActive = selectedWaypoint is not null && selectedWaypoint.PoiId == waypoint.PoiId;

            _map.Pins.Add(new Pin
            {
                Label = isActive ? $"★ {waypoint.Title}" : waypoint.Title,
                Address = waypoint.Location,
                Type = isActive ? PinType.SavedPin : PinType.Place,
                Location = location
            });
        }

        _map.MapElements.Add(polyline);
    }
}
