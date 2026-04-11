using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelApp.Models.Runtime;
using TravelApp.Services;
using TravelApp.Services.Abstractions;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class TourMapRoutePage : ContentPage, IQueryAttributable
{
    private static readonly Color RouteStartColor = Color.FromArgb("#1C6272");
    private static readonly Color[] SegmentColors =
    [
        Color.FromArgb("#E31667"),
        Color.FromArgb("#2D9CDB"),
        Color.FromArgb("#27AE60"),
        Color.FromArgb("#F2994A"),
        Color.FromArgb("#9B51E0")
    ];

    private const double PulseMinMeters = 26;
    private const double PulseMaxMeters = 86;
    private const double PulseStepMeters = 4;

    private readonly TourMapRouteViewModel _viewModel;
    private readonly ITourRouteGeometryService _routeGeometryService;
    private readonly ILocationTrackerService _locationPollingService;
    private readonly Microsoft.Maui.Controls.Maps.Map _map;
    private int? _tourId;
    private int? _poiId;
    private string? _languageCode;
    private bool _routeLoaded;
    private bool _isLoadingRoute;
    private LocationSample? _currentLocation;
    private IDispatcherTimer? _pulseTimer;
    private Circle? _activePulseCircle;
    private double _pulseRadiusMeters = PulseMinMeters;
    private bool _pulseExpanding = true;
    private RouteGeometryResult _routeGeometry = new();

    public TourMapRoutePage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<TourMapRouteViewModel>();
        _routeGeometryService = MauiProgram.Services.GetRequiredService<ITourRouteGeometryService>();
        _locationPollingService = MauiProgram.Services.GetRequiredService<ILocationTrackerService>();
        BindingContext = _viewModel;
        _viewModel.RouteChanged += OnRouteChanged;
        _locationPollingService.LocationChanged += OnLocationUpdated;
        _currentLocation = _locationPollingService.CurrentLocation;

        _map = new Microsoft.Maui.Controls.Maps.Map
        {
            MapType = MapType.Street,
            IsTrafficEnabled = false,
            IsShowingUser = true
        };

        TourMapContainer.Children.Add(_map);
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("tourId", out var tourIdValue))
        {
            return;
        }

        if (query.TryGetValue("poiId", out var poiIdValue) && int.TryParse(poiIdValue?.ToString(), out var poiId))
        {
            _poiId = poiId;
        }

        if (query.TryGetValue("lang", out var languageCodeValue))
        {
            _languageCode = languageCodeValue?.ToString();
        }

        if (int.TryParse(tourIdValue?.ToString(), out var tourId))
        {
            _tourId = tourId;
            await LoadRouteAsync();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        StartPulseTimer();
        _currentLocation = _locationPollingService.CurrentLocation;

        if (_tourId.HasValue && !_routeLoaded)
        {
            await LoadRouteAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        StopPulseTimer();
        _locationPollingService.LocationChanged -= OnLocationUpdated;
        await _viewModel.StopAsync();
        _viewModel.RouteChanged -= OnRouteChanged;
        _viewModel.Dispose();
    }

    private async Task LoadRouteAsync()
    {
        if (!_tourId.HasValue || _isLoadingRoute || _routeLoaded)
        {
            return;
        }

        _isLoadingRoute = true;
        try
        {
            await _viewModel.LoadAsync(_tourId.Value, _poiId, _languageCode ?? UserProfileService.PreferredLanguage);
            _routeGeometry = _viewModel.Tour is null
                ? new RouteGeometryResult()
                : await _routeGeometryService.GetRoadPathAsync(_viewModel.Tour, _languageCode ?? UserProfileService.PreferredLanguage);
            _routeLoaded = true;
            RenderRoute();
        }
        finally
        {
            _isLoadingRoute = false;
        }
    }

    private void OnRouteChanged(object? sender, EventArgs e)
    {
        RenderRoute();
    }

    private void RenderRoute()
    {
        _map.Pins.Clear();
        _map.MapElements.Clear();

        var waypoints = _viewModel.Waypoints;
        if (waypoints.Count == 0)
        {
            return;
        }

        var selectedWaypoint = _viewModel.SelectedWaypoint ?? waypoints.First();

        var routePoints = BuildRoutePoints(waypoints);
        MoveMapToRouteBounds(routePoints);
        RenderCurrentLocationPin();
        RenderRouteSegments();
        RenderWaypoints(waypoints, selectedWaypoint);
        ConfigurePulseCircle(selectedWaypoint);
    }

    private void RenderRouteSegments()
    {
        var segments = (_routeGeometry.Segments.Count > 0
                ? _routeGeometry.Segments
                : new[]
                {
                    new RouteGeometrySegment
                    {
                        Index = 0,
                        Label = "Route",
                        Points = _viewModel.Waypoints.Select(x => new Location(x.Latitude, x.Longitude)).ToList()
                    }
                })
            .Select(segment => new RouteGeometrySegment
            {
                Index = segment.Index,
                Label = segment.Label,
                Points = segment.Points.ToList()
            })
            .ToList();

        if (_currentLocation is not null && segments.Count > 0)
        {
            var startLocation = new Location(_currentLocation.Latitude, _currentLocation.Longitude);
            var points = segments[0].Points.ToList();
            points.Insert(0, startLocation);
            segments[0].Points = points;
        }

        foreach (var segment in segments)
        {
            if (segment.Points.Count < 2)
            {
                continue;
            }

            var color = segment.Index == 0 && _currentLocation is not null
                ? RouteStartColor
                : SegmentColors[segment.Index % SegmentColors.Length];
            var polyline = new Polyline
            {
                StrokeColor = color,
                StrokeWidth = 6
            };

            foreach (var location in segment.Points)
            {
                polyline.Geopath.Add(location);
            }

            _map.MapElements.Add(polyline);
        }
    }

    private void RenderCurrentLocationPin()
    {
        if (_currentLocation is null)
        {
            return;
        }

        _map.Pins.Add(new Pin
        {
            Label = "📍 Vị trí của bạn",
            Address = "Your current location",
            Type = PinType.Place,
            Location = new Location(_currentLocation.Latitude, _currentLocation.Longitude)
        });
    }

    private void RenderWaypoints(IReadOnlyList<TourMapWaypoint> waypoints, TourMapWaypoint selectedWaypoint)
    {
        foreach (var waypoint in waypoints)
        {
            var location = new Location(waypoint.Latitude, waypoint.Longitude);
            var isActive = selectedWaypoint.PoiId == waypoint.PoiId;
            var pin = new Pin
            {
                Label = isActive ? $"▶ {waypoint.SortOrder}. {waypoint.Title}" : $"{waypoint.SortOrder}. {waypoint.Title}",
                Address = waypoint.Location,
                Type = isActive ? PinType.SavedPin : PinType.Place,
                Location = location
            };

            pin.InfoWindowClicked += (_, _) =>
            {
                if (_viewModel.SelectWaypointCommand is Command command)
                {
                    command.Execute(waypoint);
                }
            };

            _map.Pins.Add(pin);
        }
    }

    private void MoveMapToRouteBounds(IReadOnlyList<Location> points)
    {
        if (points.Count == 0)
        {
            return;
        }

        var minLat = points.Min(x => x.Latitude);
        var maxLat = points.Max(x => x.Latitude);
        var minLng = points.Min(x => x.Longitude);
        var maxLng = points.Max(x => x.Longitude);

        var center = new Location((minLat + maxLat) / 2d, (minLng + maxLng) / 2d);
        var latSpanKm = Math.Max(0.8, (maxLat - minLat) * 111.32 * 1.35);
        var lngSpanKm = Math.Max(0.8, (maxLng - minLng) * 111.32 * 1.35);
        var radiusKm = Math.Max(latSpanKm, lngSpanKm) / 2d;

        _map.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(Math.Min(10d, Math.Max(0.9d, radiusKm)))));
    }

    private void ConfigurePulseCircle(TourMapWaypoint selectedWaypoint)
    {
        if (_activePulseCircle is not null)
        {
            _map.MapElements.Remove(_activePulseCircle);
            _activePulseCircle = null;
        }

        var location = new Location(selectedWaypoint.Latitude, selectedWaypoint.Longitude);
        _activePulseCircle = new Circle
        {
            Center = location,
            Radius = Distance.FromMeters(PulseMinMeters),
            StrokeColor = Color.FromArgb("#E31667"),
            StrokeWidth = 2,
            FillColor = Color.FromArgb("#22E31667")
        };

        _map.MapElements.Add(_activePulseCircle);
        _pulseRadiusMeters = PulseMinMeters;
        _pulseExpanding = true;
    }

    private void StartPulseTimer()
    {
        if (_pulseTimer is not null)
        {
            _pulseTimer.Start();
            return;
        }

        _pulseTimer = Dispatcher.CreateTimer();
        _pulseTimer.Interval = TimeSpan.FromMilliseconds(220);
        _pulseTimer.Tick += OnPulseTimerTick;
        _pulseTimer.Start();
    }

    private void StopPulseTimer()
    {
        if (_pulseTimer is null)
        {
            return;
        }

        _pulseTimer.Stop();
        _pulseTimer.Tick -= OnPulseTimerTick;
        _pulseTimer = null;
    }

    private void OnPulseTimerTick(object? sender, EventArgs e)
    {
        if (_activePulseCircle is null)
        {
            return;
        }

        if (_pulseExpanding)
        {
            _pulseRadiusMeters += PulseStepMeters;
            if (_pulseRadiusMeters >= PulseMaxMeters)
            {
                _pulseRadiusMeters = PulseMaxMeters;
                _pulseExpanding = false;
            }
        }
        else
        {
            _pulseRadiusMeters -= PulseStepMeters;
            if (_pulseRadiusMeters <= PulseMinMeters)
            {
                _pulseRadiusMeters = PulseMinMeters;
                _pulseExpanding = true;
            }
        }

        _activePulseCircle.Radius = Distance.FromMeters(_pulseRadiusMeters);
    }

    private List<Location> BuildRoutePoints(IReadOnlyList<TourMapWaypoint> waypoints)
    {
        var points = new List<Location>();

        if (_currentLocation is not null)
        {
            points.Add(new Location(_currentLocation.Latitude, _currentLocation.Longitude));
        }

        points.AddRange(waypoints.Select(x => new Location(x.Latitude, x.Longitude)));
        return points;
    }

    private void OnLocationUpdated(object? sender, LocationSample sample)
    {
        _currentLocation = sample;
        MainThread.BeginInvokeOnMainThread(RenderRoute);
    }
}
