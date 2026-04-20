using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Runtime;
using TravelApp.Models.Contracts;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class TourMapRouteViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ITourRouteCatalogService _tourRouteCatalogService;
    private readonly ITourRoutePlaybackService _tourRoutePlaybackService;
    private readonly IAnalyticsTrackingService _analyticsTrackingService;
    private string _statusText = AppStrings.LoadingRoute;
    private int? _selectedPoiId;
    private int? _anchorPoiId;
    private string? _loadedLanguageCode;
    private bool _isLoading;

    public ObservableCollection<TourMapWaypoint> Waypoints { get; } = [];

    public TourMapWaypoint? SelectedWaypoint { get; private set; }
    public LocationSample? CurrentLocation { get; private set; }
    public TourRouteDto? Tour { get; private set; }
    public string PageTitle => AppStrings.TourMapRouteTitle;
    public bool HasActiveWaypoint => SelectedWaypoint is not null;
    public string CurrentWaypointTitle => SelectedWaypoint?.Title ?? AppStrings.NoWaypointTitle;
    public string CurrentWaypointSubtitle => SelectedWaypoint?.Location ?? AppStrings.GoToFirstPoint;
    public string CurrentWaypointProgressText => Waypoints.Count == 0 || SelectedWaypoint is null
        ? "0/0"
        : $"{SelectedWaypoint.SortOrder}/{Waypoints.Count}";
    public string CurrentWaypointDistanceText => SelectedWaypoint?.DistanceMeters is null
        ? string.Empty
        : string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0} {1:F0} m", AppStrings.RouteDistancePrefix, SelectedWaypoint.DistanceMeters);
    public string PlayingText => AppStrings.AudioNowPlaying;
    public string StopsText => AppStrings.RouteStopsLabel;
    public string OpenText => AppStrings.Open;
    public string PlayingBadgeText => AppStrings.AudioNowPlaying;
    public double CurrentWaypointProgressValue => Waypoints.Count == 0 || SelectedWaypoint is null
        ? 0
        : (double)SelectedWaypoint.SortOrder / Waypoints.Count;

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

    public ICommand BackCommand { get; }
    public ICommand OpenPoiCommand { get; }
    public ICommand SelectWaypointCommand { get; }
    public ICommand RecenterCommand { get; }

    public event EventHandler? RouteChanged;

    public TourMapRouteViewModel(ITourRouteCatalogService tourRouteCatalogService, ITourRoutePlaybackService tourRoutePlaybackService, IAnalyticsTrackingService analyticsTrackingService)
    {
        _tourRouteCatalogService = tourRouteCatalogService;
        _tourRoutePlaybackService = tourRoutePlaybackService;
        _analyticsTrackingService = analyticsTrackingService;
        _tourRoutePlaybackService.ActiveWaypointChanged += OnActiveWaypointChanged;
        UserProfileService.ProfileChanged += OnProfileChanged;

        BackCommand = new Command(async () =>
        {
            await _tourRoutePlaybackService.StopAsync();
            await Shell.Current.GoToAsync("..");
        });
        SelectWaypointCommand = new Command<TourMapWaypoint>(async waypoint => await SelectWaypointAsync(waypoint));
        OpenPoiCommand = new Command<TourMapWaypoint>(async waypoint =>
        {
            if (waypoint is null)
            {
                return;
            }

            await _tourRoutePlaybackService.StopAsync();

            var languageCode = Uri.EscapeDataString(_loadedLanguageCode ?? UserProfileService.PreferredLanguage);
            await Shell.Current.GoToAsync($"TourDetailPage?tourId={waypoint.PoiId}&lang={languageCode}");
        });
        RecenterCommand = new Command(() => RouteChanged?.Invoke(this, EventArgs.Empty));
    }

    public async Task LoadAsync(int anchorPoiId, int? preferredPoiId = null, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = string.IsNullOrWhiteSpace(languageCode) ? UserProfileService.PreferredLanguage : languageCode.Trim().ToLowerInvariant();

        if (_anchorPoiId == anchorPoiId && Waypoints.Count > 0 && string.Equals(_loadedLanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
        {
            if (preferredPoiId.HasValue)
            {
                var existingWaypoint = Waypoints.FirstOrDefault(x => x.PoiId == preferredPoiId.Value);
                if (existingWaypoint is not null && SelectedWaypoint?.PoiId != existingWaypoint.PoiId)
                {
                    await _tourRoutePlaybackService.SelectWaypointAsync(existingWaypoint.PoiId, cancellationToken);
                }
            }

            return;
        }

        IsLoading = true;
        StatusText = AppStrings.LoadingRoute;

        try
        {
            var route = await _tourRouteCatalogService.GetRouteAsync(anchorPoiId, normalizedLanguage, cancellationToken);
            if (route is null)
            {
                Tour = null;
                Waypoints.Clear();
                SelectedWaypoint = null;
                CurrentLocation = null;
                _selectedPoiId = null;
                _anchorPoiId = null;
                _loadedLanguageCode = null;
                StatusText = AppStrings.NoWaypointTitle;
                OnPropertyChanged(nameof(Tour));
                RouteChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            Tour = route;
            _anchorPoiId = anchorPoiId;
            _loadedLanguageCode = normalizedLanguage;
            CurrentLocation = null;

            Waypoints.Clear();
            foreach (var (waypoint, index) in route.Waypoints.Select((x, i) => (x, i)))
            {
                Waypoints.Add(new TourMapWaypoint
                {
                    PoiId = waypoint.Poi.Id,
                    SortOrder = waypoint.SortOrder == 0 ? index + 1 : waypoint.SortOrder,
                    Title = waypoint.Poi.Title,
                    Location = waypoint.Poi.Location,
                    Latitude = waypoint.Poi.Latitude,
                    Longitude = waypoint.Poi.Longitude,
                    DistanceMeters = waypoint.DistanceFromPreviousMeters,
                    Poi = waypoint.Poi
                });
            }

            var preferredWaypoint = preferredPoiId.HasValue
                ? Waypoints.FirstOrDefault(x => x.PoiId == preferredPoiId.Value)
                : null;
            SetSelectedWaypoint(preferredWaypoint ?? Waypoints.FirstOrDefault(), raiseRouteChanged: false);
            StatusText = Waypoints.Count == 0
                ? AppStrings.NoWaypointTitle
                : $"{Waypoints.Count} {AppStrings.RouteStopsLabel} • {route.TotalDistanceMeters / 1000d:0.0} km";

            await _tourRoutePlaybackService.StartAsync(route, preferredPoiId, cancellationToken);
            _ = _analyticsTrackingService.TrackTourViewedAsync(route.Id, preferredPoiId, normalizedLanguage, cancellationToken);
            OnPropertyChanged(nameof(Tour));
            RouteChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Tour = null;
            Waypoints.Clear();
            SelectedWaypoint = null;
            CurrentLocation = null;
            _selectedPoiId = null;
            _anchorPoiId = null;
            _loadedLanguageCode = null;
            StatusText = $"{AppStrings.LoadingRoute}: {ex.Message}";
            OnPropertyChanged(nameof(Tour));
            RouteChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _tourRoutePlaybackService.StopAsync(cancellationToken);
    }

    private async Task SelectWaypointAsync(TourMapWaypoint? waypoint)
    {
        if (waypoint is not null)
        {
            SetSelectedWaypoint(waypoint, raiseRouteChanged: true);
            await _tourRoutePlaybackService.SelectWaypointAsync(waypoint.PoiId);
        }
    }

    private void OnActiveWaypointChanged(object? sender, TourRoutePlaybackChangedEventArgs e)
    {
        if (e.UserLocation is not null)
        {
            CurrentLocation = e.UserLocation;
            OnPropertyChanged(nameof(CurrentLocation));
        }

        var waypoint = e.Waypoint is null
            ? null
            : Waypoints.FirstOrDefault(x => x.PoiId == e.Waypoint.Poi.Id);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetSelectedWaypoint(waypoint, raiseRouteChanged: true);
            if (e.UserLocation is not null && waypoint is not null)
            {
                StatusText = $"Đang ở điểm {waypoint.SortOrder} • {waypoint.Title}";
            }
        });
    }

    private void SetSelectedWaypoint(TourMapWaypoint? waypoint, bool raiseRouteChanged)
    {
        _selectedPoiId = waypoint?.PoiId;

        var refreshed = Waypoints.Select(item => new TourMapWaypoint
        {
            PoiId = item.PoiId,
            SortOrder = item.SortOrder,
            Title = item.Title,
            Location = item.Location,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            DistanceMeters = item.DistanceMeters,
            Poi = item.Poi,
            IsActive = item.PoiId == _selectedPoiId
        }).ToList();

        Waypoints.Clear();
        foreach (var item in refreshed)
        {
            Waypoints.Add(item);
        }

        SelectedWaypoint = Waypoints.FirstOrDefault(x => x.PoiId == _selectedPoiId);

        OnPropertyChanged(nameof(SelectedWaypoint));
        RaiseRouteStateChanged();

        if (raiseRouteChanged)
        {
            RouteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RaiseRouteStateChanged()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(HasActiveWaypoint));
        OnPropertyChanged(nameof(CurrentWaypointTitle));
        OnPropertyChanged(nameof(CurrentWaypointSubtitle));
        OnPropertyChanged(nameof(CurrentWaypointProgressText));
        OnPropertyChanged(nameof(CurrentWaypointDistanceText));
        OnPropertyChanged(nameof(CurrentWaypointProgressValue));
        OnPropertyChanged(nameof(PlayingText));
        OnPropertyChanged(nameof(StopsText));
        OnPropertyChanged(nameof(OpenText));
        OnPropertyChanged(nameof(PlayingBadgeText));
    }

    public void Dispose()
    {
        _tourRoutePlaybackService.ActiveWaypointChanged -= OnActiveWaypointChanged;
        UserProfileService.ProfileChanged -= OnProfileChanged;
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        var currentLanguage = string.IsNullOrWhiteSpace(UserProfileService.PreferredLanguage)
            ? null
            : UserProfileService.PreferredLanguage.Trim().ToLowerInvariant();

        if (_anchorPoiId.HasValue
            && Waypoints.Count > 0
            && !string.IsNullOrWhiteSpace(currentLanguage)
            && !string.Equals(_loadedLanguageCode, currentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            _ = LoadAsync(_anchorPoiId.Value, _selectedPoiId, currentLanguage);
            return;
        }

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(CurrentWaypointTitle));
        OnPropertyChanged(nameof(CurrentWaypointSubtitle));
        OnPropertyChanged(nameof(CurrentWaypointDistanceText));
        OnPropertyChanged(nameof(PlayingText));
        OnPropertyChanged(nameof(StopsText));
        OnPropertyChanged(nameof(OpenText));
        OnPropertyChanged(nameof(PlayingBadgeText));
    }
}
