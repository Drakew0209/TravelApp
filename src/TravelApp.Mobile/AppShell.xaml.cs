namespace TravelApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("SearchPage", typeof(SearchPage));
            Routing.RegisterRoute("TourDetailPage", typeof(TourDetailPage));
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            Routing.RegisterRoute("EditProfilePage", typeof(EditProfilePage));
            Routing.RegisterRoute("DebugRuntimeConsolePage", typeof(DebugRuntimeConsolePage));
            Routing.RegisterRoute("PoiListPage", typeof(PoiListPage));
            Routing.RegisterRoute("NowPlayingPage", typeof(NowPlayingPage));
            Routing.RegisterRoute("MyAudioLibraryPage", typeof(MyAudioLibraryPage));
            Routing.RegisterRoute("BookmarksHistoryPage", typeof(BookmarksHistoryPage));
            Routing.RegisterRoute("TourMapRoutePage", typeof(TourMapRoutePage));
            Routing.RegisterRoute("MapPage", typeof(MapPage));
        }
    }
}
