using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class ProfileViewModel : INotifyPropertyChanged
{
    private readonly IProfileApiClient _profileApiClient;
    private readonly IAuthApiClient _authApiClient;
    private readonly IAudioLibraryService _audioLibraryService;
    private int _offlineDownloadsCount;

    public bool IsLoggedIn => AuthStateService.IsLoggedIn;

    public string GreetingTitle => IsLoggedIn ? $"Hi, {UserProfileService.FullName}" : "Welcome to TravelApp";
    public string GreetingSubtitle => IsLoggedIn ? UserProfileService.Email : "Sign in to manage downloads, bookmarks and your profile.";
    public string PrimaryActionText => IsLoggedIn ? "Sign Out" : "Sign In";

    public bool ShowAccountSection => IsLoggedIn;
    public bool ShowPurchases => IsLoggedIn;
    public bool ShowDownloads => IsLoggedIn;
    public bool ShowBookmarks => IsLoggedIn;
    public string DownloadsTitle => _offlineDownloadsCount > 0 ? $"Downloads ({_offlineDownloadsCount})" : "Downloads";

    public ICommand BackCommand { get; }
    public ICommand PrimaryActionCommand { get; }
    public ICommand OpenEditProfileCommand { get; }
    public ICommand OpenDownloadsCommand { get; }
    public ICommand OpenBookmarksCommand { get; }

    public ProfileViewModel(IProfileApiClient profileApiClient, IAuthApiClient authApiClient, IAudioLibraryService audioLibraryService)
    {
        _profileApiClient = profileApiClient;
        _authApiClient = authApiClient;
        _audioLibraryService = audioLibraryService;

        AuthStateService.AuthStateChanged += OnAuthStateChanged;
        UserProfileService.ProfileChanged += OnProfileChanged;
        _audioLibraryService.LibraryChanged += async (_, _) => await RefreshOfflineDownloadsCountAsync();

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        OpenEditProfileCommand = new Command(async () => await Shell.Current.GoToAsync("EditProfilePage"));
        OpenDownloadsCommand = new Command(async () => await Shell.Current.GoToAsync("MyAudioLibraryPage"));
        OpenBookmarksCommand = new Command(async () => await Shell.Current.GoToAsync("BookmarksHistoryPage?tab=bookmarks"));
        PrimaryActionCommand = new Command(async () =>
        {
            if (IsLoggedIn)
            {
                await _authApiClient.LogoutAsync();
                AuthStateService.IsLoggedIn = false;
            }
            else
            {
                await Shell.Current.GoToAsync("LoginPage");
            }
        });

        if (IsLoggedIn)
        {
            _ = LoadProfileAsync();
        }

        _ = RefreshOfflineDownloadsCountAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnAuthStateChanged(object? sender, EventArgs e)
    {
        RaiseAuthBoundProperties();

        if (IsLoggedIn)
        {
            _ = LoadProfileAsync();
        }
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(GreetingTitle));
        OnPropertyChanged(nameof(GreetingSubtitle));
    }

    private void RaiseAuthBoundProperties()
    {
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(GreetingTitle));
        OnPropertyChanged(nameof(GreetingSubtitle));
        OnPropertyChanged(nameof(PrimaryActionText));
        OnPropertyChanged(nameof(ShowAccountSection));
        OnPropertyChanged(nameof(ShowPurchases));
        OnPropertyChanged(nameof(ShowDownloads));
        OnPropertyChanged(nameof(ShowBookmarks));
        OnPropertyChanged(nameof(DownloadsTitle));
    }

    private async Task RefreshOfflineDownloadsCountAsync()
    {
        var count = await _audioLibraryService.GetDownloadedCountAsync(UserProfileService.PreferredLanguage);
        if (_offlineDownloadsCount == count)
        {
            return;
        }

        _offlineDownloadsCount = count;
        MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(DownloadsTitle)));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            var profile = await _profileApiClient.GetMyProfileAsync();
            if (profile is null)
                return;

            UserProfileService.Email = profile.Email;
            UserProfileService.FullName = profile.FullName;
            UserProfileService.CountryCode = profile.CountryCode;
            UserProfileService.PhoneNumber = profile.PhoneNumber;
            UserProfileService.PreferredLanguage = profile.PreferredLanguage;
        }
        catch
        {
        }
    }
}
