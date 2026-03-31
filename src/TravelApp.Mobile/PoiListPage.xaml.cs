using Microsoft.Extensions.DependencyInjection;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class PoiListPage : ContentPage
{
    private readonly PoiListViewModel _viewModel;

    public PoiListPage()
    {
        InitializeComponent();
        _viewModel = MauiProgram.Services.GetRequiredService<PoiListViewModel>();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.StartPollingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ = _viewModel.StopPollingAsync();
    }
}
