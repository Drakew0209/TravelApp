using Microsoft.Extensions.DependencyInjection;
using TravelApp.ViewModels;

namespace TravelApp;

public partial class NetworkSettingsPage : ContentPage
{
    public NetworkSettingsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<NetworkSettingsViewModel>();
    }
}
