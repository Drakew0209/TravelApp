using Microsoft.Extensions.DependencyInjection;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class MyAudioLibraryPage : ContentPage
{
    private readonly MyAudioLibraryViewModel _viewModel;

    public MyAudioLibraryPage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<MyAudioLibraryViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.StopAsync();
        _viewModel.Dispose();
    }
}
