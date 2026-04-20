using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly Task _endpointInitializationTask;

        public App()
        {
            InitializeComponent();
            UserProfileService.ApplyPreferredLanguageCulture();
            _ = MauiProgram.Services.GetRequiredService<TravelApp.Services.Abstractions.IAudioService>();
            _endpointInitializationTask = InitializeEndpointSettingsAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(CreateLoadingPage());
            _ = ActivateShellWhenReadyAsync(window);
            return window;
        }

        private static ContentPage CreateLoadingPage()
        {
            return new ContentPage
            {
                BackgroundColor = Colors.White,
                Content = new Grid
                {
                    Padding = 24,
                    RowDefinitions =
                    {
                        new RowDefinition(GridLength.Star),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Star)
                    },
                    Children =
                    {
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            Color = Color.FromArgb("#E31667"),
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = AppStrings.LoadingTitle,
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#1B1F28"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = AppStrings.LoadingSubtitle,
                            FontSize = 13,
                            TextColor = Color.FromArgb("#5D6472"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                }
            };
        }

        private async Task ActivateShellWhenReadyAsync(Window window)
        {
            try
            {
                await _endpointInitializationTask;
            }
            catch
            {
            }

            MainThread.BeginInvokeOnMainThread(() => window.Page = MauiProgram.Services.GetRequiredService<AppShell>());
        }

        private static async Task InitializeEndpointSettingsAsync()
        {
            try
            {
                var endpointSettings = MauiProgram.Services.GetRequiredService<IEndpointSettingsService>();
                var discovery = MauiProgram.Services.GetRequiredService<ILanEndpointDiscoveryService>();

                if (await IsEndpointReachableAsync(endpointSettings.ApiBaseUrl))
                {
                    return;
                }

                if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType == DeviceType.Virtual)
                {
                    endpointSettings.Update("http://10.0.2.2:5293/", "http://10.0.2.2:5175/");
                    return;
                }

                if (IsPlaceholderEndpoint(endpointSettings.ApiBaseUrl) || IsPlaceholderEndpoint(endpointSettings.PublicWebBaseUrl))
                {
                    var result = await discovery.TryDiscoverAsync();
                    if (result is not null)
                    {
                        endpointSettings.Update(result.ApiBaseUrl, result.PublicWebBaseUrl);
                    }
                }
            }
            catch
            {
            }
        }

        private static bool IsPlaceholderEndpoint(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return true;
            }

            return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(uri.Host, "10.0.2.2", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(uri.Host, "0.0.0.0", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<bool> IsEndpointReachableAsync(string? apiBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(apiBaseUrl) || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
                using var response = await client.GetAsync(new Uri(uri, "health"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}