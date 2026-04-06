using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TravelApp.Services.Abstractions;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class ExplorePage : ContentPage
{
    private readonly ExploreViewModel _viewModel;
    private readonly ITravelBootstrapService _travelBootstrapService;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly ILogger<ExplorePage> _logger;
    private IDispatcherTimer? _audioStatusTimer;

    public ExplorePage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<ExploreViewModel>();
        BindingContext = _viewModel;
        _travelBootstrapService = MauiProgram.Services.GetRequiredService<ITravelBootstrapService>();
        _audioPlayerService = MauiProgram.Services.GetRequiredService<IAudioPlayerService>();
        _logger = MauiProgram.Services.GetRequiredService<ILogger<ExplorePage>>();
        InitializeMap();
        UpdateAudioStatus();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ResetBottomTabToExplore();
        StartAudioStatusTimer();
        _ = StartRuntimeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAudioStatusTimer();
        _ = StopRuntimeAsync();
    }

    private void InitializeMap()
    {
#if WINDOWS
        if (string.IsNullOrWhiteSpace(Windows.Services.Maps.MapService.ServiceToken))
        {
            _logger.LogWarning("Windows map token is missing. Set BING_MAPS_KEY to show map on Windows.");
            var placeholderContainer = this.FindByName<Grid>("ExploreMapContainer");
            if (placeholderContainer is not null)
            {
                placeholderContainer.Children.Clear();
                placeholderContainer.Children.Add(new Label
                {
                    Text = "Map needs BING_MAPS_KEY on Windows.",
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = Colors.Gray,
                    Margin = new Thickness(12)
                });
            }

            return;
        }
#endif

        InitializeMapInContainer("ExploreMapContainer", showUser: false, radiusKm: 1);
        InitializeMapInContainer("DiscoverMapContainer", showUser: true, radiusKm: 1.2);
    }

    private void InitializeMapInContainer(string containerName, bool showUser, double radiusKm)
    {
        var map = new Microsoft.Maui.Controls.Maps.Map
        {
            MapType = MapType.Street,
            IsTrafficEnabled = false,
            IsShowingUser = showUser
        };

        var mapContainer = this.FindByName<Grid>(containerName);
        if (mapContainer is null)
        {
            _logger.LogWarning("Map container '{ContainerName}' was not found in XAML.", containerName);
            return;
        }

        mapContainer.Children.Clear();
        mapContainer.Children.Add(map);

        var hoChiMinhCity = new Location(10.7769, 106.7009);
        map.MoveToRegion(MapSpan.FromCenterAndRadius(hoChiMinhCity, Distance.FromKilometers(radiusKm)));

        map.Pins.Add(new Pin
        {
            Label = "Ho Chi Minh City",
            Address = "District 1",
            Type = PinType.Place,
            Location = hoChiMinhCity
        });
    }

    private async Task StartRuntimeAsync()
    {
        try
        {
            await _travelBootstrapService.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Explore page: failed to start runtime pipeline.");
        }
    }

    private async Task StopRuntimeAsync()
    {
        try
        {
            await _travelBootstrapService.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Explore page: failed to stop runtime pipeline.");
        }
    }

    private void StartAudioStatusTimer()
    {
        if (_audioStatusTimer is not null)
        {
            _audioStatusTimer.Start();
            return;
        }

        _audioStatusTimer = Dispatcher.CreateTimer();
        _audioStatusTimer.Interval = TimeSpan.FromMilliseconds(500);
        _audioStatusTimer.Tick += OnAudioStatusTick;
        _audioStatusTimer.Start();
    }

    private void StopAudioStatusTimer()
    {
        if (_audioStatusTimer is null)
        {
            return;
        }

        _audioStatusTimer.Stop();
        _audioStatusTimer.Tick -= OnAudioStatusTick;
        _audioStatusTimer = null;

        AudioStatusBorder.IsVisible = false;
    }

    private void OnAudioStatusTick(object? sender, EventArgs e)
    {
        UpdateAudioStatus();
    }

    private void UpdateAudioStatus()
    {
        var isPlaying = _audioPlayerService.IsPlaying;
        AudioStatusBorder.IsVisible = isPlaying;

        if (!isPlaying)
        {
            AudioStatusTextLabel.Text = "";
            AudioPoiTitleLabel.Text = "";
            return;
        }

        AudioStatusTextLabel.Text = "Đang phát audio";
        AudioPoiTitleLabel.Text = _audioPlayerService.CurrentPoiTitle ?? "Địa điểm hiện tại";
    }
}
