using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Runtime;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class TourMapRouteViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ITourMapRouteService _tourMapRouteService;
    private string _statusText = "Đang lấy route...";
    private int? _selectedPoiId;

    public ObservableCollection<TourMapWaypoint> Waypoints { get; } = [];

    public TourMapRouteSnapshot? Snapshot { get; private set; }
    public TourMapWaypoint? SelectedWaypoint { get; private set; }

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

    public ICommand BackCommand { get; }
    public ICommand OpenPoiCommand { get; }
    public ICommand SelectWaypointCommand { get; }

    public event EventHandler? RouteChanged;

    public TourMapRouteViewModel(ITourMapRouteService tourMapRouteService)
    {
        _tourMapRouteService = tourMapRouteService;
        _tourMapRouteService.RouteUpdated += OnRouteUpdated;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        SelectWaypointCommand = new Command<TourMapWaypoint>(SelectWaypoint);
        OpenPoiCommand = new Command<TourMapWaypoint>(async waypoint =>
        {
            if (waypoint is null)
            {
                return;
            }

            SelectWaypoint(waypoint);

            await Shell.Current.GoToAsync($"TourDetailPage?tourId={waypoint.PoiId}");
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _tourMapRouteService.StartAsync(UserProfileService.PreferredLanguage, cancellationToken);

        if (_tourMapRouteService.CurrentSnapshot is not null)
        {
            ApplySnapshot(_tourMapRouteService.CurrentSnapshot);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _tourMapRouteService.StopAsync(cancellationToken);
    }

    private void OnRouteUpdated(object? sender, TourMapRouteUpdatedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => ApplySnapshot(e.Snapshot));
    }

    private void ApplySnapshot(TourMapRouteSnapshot snapshot)
    {
        Snapshot = snapshot;

        Waypoints.Clear();
        foreach (var waypoint in snapshot.Waypoints)
        {
            Waypoints.Add(waypoint);
        }

        var restoredSelection = Waypoints.FirstOrDefault(x => x.PoiId == _selectedPoiId) ?? Waypoints.FirstOrDefault();
        SetSelectedWaypoint(restoredSelection, raiseRouteChanged: false);

        StatusText = snapshot.Waypoints.Count == 0
            ? "Chưa có điểm route gần bạn."
            : $"{snapshot.Waypoints.Count} điểm route gần bạn • cập nhật {snapshot.UpdatedAtUtc:HH:mm:ss}";

        RouteChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(Snapshot));
        OnPropertyChanged(nameof(StatusText));
    }

    private void SelectWaypoint(TourMapWaypoint? waypoint)
    {
        SetSelectedWaypoint(waypoint, raiseRouteChanged: true);
    }

    private void SetSelectedWaypoint(TourMapWaypoint? waypoint, bool raiseRouteChanged)
    {
        _selectedPoiId = waypoint?.PoiId;
        SelectedWaypoint = waypoint;

        foreach (var item in Waypoints)
        {
            item.IsActive = item.PoiId == _selectedPoiId;
        }

        OnPropertyChanged(nameof(SelectedWaypoint));
        OnPropertyChanged(nameof(Waypoints));

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

    public void Dispose()
    {
        _tourMapRouteService.RouteUpdated -= OnRouteUpdated;
    }
}
