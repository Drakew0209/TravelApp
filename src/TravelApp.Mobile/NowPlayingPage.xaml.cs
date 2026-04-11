using Microsoft.Extensions.DependencyInjection;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class NowPlayingPage : ContentPage
{
    private readonly NowPlayingViewModel _viewModel;

    public NowPlayingPage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<NowPlayingViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.StopAsync();
        _viewModel.Dispose();
    }
}
