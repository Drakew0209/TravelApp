using TravelApp.Resources.Strings;
using TravelApp.Services;

namespace TravelApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            ApplyLocalizedTitles();
            UserProfileService.ProfileChanged += OnProfileChanged;
            Routing.RegisterRoute("SearchPage", typeof(SearchPage));
            Routing.RegisterRoute("TourDetailPage", typeof(TourDetailPage));
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            Routing.RegisterRoute("EditProfilePage", typeof(EditProfilePage));
            Routing.RegisterRoute("DebugRuntimeConsolePage", typeof(DebugRuntimeConsolePage));
            Routing.RegisterRoute("PoiListPage", typeof(PoiListPage));
            Routing.RegisterRoute("NowPlayingPage", typeof(NowPlayingPage));
            Routing.RegisterRoute("MyAudioLibraryPage", typeof(MyAudioLibraryPage));
            Routing.RegisterRoute("BookmarksHistoryPage", typeof(BookmarksHistoryPage));
            Routing.RegisterRoute("TourMapRoutePage", typeof(TourMapRoutePage));
            Routing.RegisterRoute("MapPage", typeof(MapPage));
            Routing.RegisterRoute("QrScannerPage", typeof(QrScannerPage));
            Routing.RegisterRoute("NetworkSettingsPage", typeof(NetworkSettingsPage));
        }

        private void OnProfileChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(ApplyLocalizedTitles);
        }

        private void ApplyLocalizedTitles()
        {
            Title = AppStrings.AppName;
            if (GetShellContent("ExplorePage") is not null)
            {
                GetShellContent("ExplorePage")!.Title = AppStrings.Explore;
            }

            if (GetShellContent("MapPage") is not null)
            {
                GetShellContent("MapPage")!.Title = AppStrings.Map;
            }
        }

        private ShellContent? GetShellContent(string route)
        {
            return Items
                .OfType<ShellItem>()
                .SelectMany(item => item.Items.OfType<ShellSection>())
                .SelectMany(section => section.Items.OfType<ShellContent>())
                .FirstOrDefault(content => string.Equals(content.Route, route, StringComparison.OrdinalIgnoreCase));
        }
    }
}
