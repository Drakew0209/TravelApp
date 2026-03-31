using Microsoft.Extensions.DependencyInjection;

namespace TravelApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            _ = MauiProgram.Services.GetRequiredService<TravelApp.Services.Abstractions.IAudioService>();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(MauiProgram.Services.GetRequiredService<AppShell>());
        }
    }
}