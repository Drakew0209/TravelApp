using Microsoft.Extensions.DependencyInjection;
using TravelApp.Services.Abstractions;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class TourDetailPage : ContentPage, IQueryAttributable
{
    private readonly TourDetailViewModel _viewModel;
    private readonly IAudioService _audioService;
    private bool _isFirstAppearing = true;

    public TourDetailPage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<TourDetailViewModel>();
        _audioService = MauiProgram.Services.GetRequiredService<IAudioService>();
        BindingContext = _viewModel;
        Appearing += OnPageAppearing;
        Disappearing += OnPageDisappearing;
    }

    private async void OnPageAppearing(object? sender, EventArgs e)
    {
        if (_isFirstAppearing)
        {
            _isFirstAppearing = false;
            return;
        }

        await _viewModel.RefreshAsync();
    }

    private async void OnPageDisappearing(object? sender, EventArgs e)
    {
        await _viewModel.StopAsync();
        _viewModel.Dispose();
        await _audioService.StopAsync();
    }

    private async void OnSpeechTextEditorUnfocused(object? sender, FocusEventArgs e)
    {
        await _viewModel.PersistSpeechTextAsync();
    }

    private async void OnBookmarkTapped(object? sender, TappedEventArgs e)
    {
        if (sender is VisualElement element)
        {
            await element.ScaleTo(0.92, 90, Easing.CubicOut);
            await element.ScaleTo(1.0, 120, Easing.CubicOut);
        }

        if (_viewModel.ToggleBookmarkCommand.CanExecute(null))
        {
            _viewModel.ToggleBookmarkCommand.Execute(null);
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("tourId", out var tourId))
        {
            _viewModel.Load(tourId?.ToString());
        }
    }
}
