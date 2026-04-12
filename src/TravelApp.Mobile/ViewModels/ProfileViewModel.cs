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
    private readonly ILocalDatabaseService _localDatabaseService;
    private int _offlineDownloadsCount;
    private string _backupStatusText = string.Empty;

    public bool IsLoggedIn => AuthStateService.IsLoggedIn;

    public string GreetingTitle => IsLoggedIn
        ? string.IsNullOrWhiteSpace(UserProfileService.FullName)
            ? "Hi"
            : $"Hi, {UserProfileService.FullName}"
        : "Welcome to TravelApp";

    public string GreetingSubtitle => IsLoggedIn
        ? string.IsNullOrWhiteSpace(UserProfileService.Email)
            ? "Your account is ready."
            : UserProfileService.Email
        : "Sign in to manage downloads, bookmarks and your profile.";
    public string PrimaryActionText => IsLoggedIn ? "Sign Out" : "Sign In";

    public bool ShowAccountSection => IsLoggedIn;
    public bool ShowPurchases => IsLoggedIn;
    public bool ShowDownloads => IsLoggedIn;
    public bool ShowBookmarks => IsLoggedIn;
    public string DownloadsTitle => _offlineDownloadsCount > 0 ? $"Downloads ({_offlineDownloadsCount})" : "Downloads";
    public string BackupStatus => _backupStatusText;

    public ICommand BackCommand { get; }
    public ICommand PrimaryActionCommand { get; }
    public ICommand OpenEditProfileCommand { get; }
    public ICommand OpenDownloadsCommand { get; }
    public ICommand OpenBookmarksCommand { get; }
    public ICommand ExportDatabaseCommand { get; }
    public ICommand ImportDatabaseCommand { get; }

    public ProfileViewModel(IProfileApiClient profileApiClient, IAuthApiClient authApiClient, IAudioLibraryService audioLibraryService, ILocalDatabaseService localDatabaseService)
    {
        _profileApiClient = profileApiClient;
        _authApiClient = authApiClient;
        _audioLibraryService = audioLibraryService;
        _localDatabaseService = localDatabaseService;

        AuthStateService.AuthStateChanged += OnAuthStateChanged;
        UserProfileService.ProfileChanged += OnProfileChanged;
        _audioLibraryService.LibraryChanged += async (_, _) => await RefreshOfflineDownloadsCountAsync();

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        OpenEditProfileCommand = new Command(async () => await Shell.Current.GoToAsync("EditProfilePage"));
        OpenDownloadsCommand = new Command(async () => await Shell.Current.GoToAsync("MyAudioLibraryPage"));
        OpenBookmarksCommand = new Command(async () => await Shell.Current.GoToAsync("BookmarksHistoryPage?tab=bookmarks"));
        ExportDatabaseCommand = new Command(async () => await ExportDatabaseAsync());
        ImportDatabaseCommand = new Command(async () => await ImportDatabaseAsync());
        PrimaryActionCommand = new Command(async () =>
        {
            if (IsLoggedIn)
            {
                await _authApiClient.LogoutAsync();
                UserProfileService.SetRoles(Array.Empty<string>());
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

    private async Task ExportDatabaseAsync()
    {
        try
        {
            var exportDirectory = Path.Combine(FileSystem.CacheDirectory, "travelapp-backup");
            var exportedPath = await _localDatabaseService.ExportDatabaseAsync(exportDirectory);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export TravelApp database",
                File = new ShareFile(exportedPath)
            });

            UpdateBackupStatus($"Đã export database: {Path.GetFileName(exportedPath)}");
        }
        catch (Exception ex)
        {
            UpdateBackupStatus($"Export thất bại: {ex.Message}");
        }
    }

    private async Task ImportDatabaseAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Chọn file travelapp-local.db3"
            });

            if (file is null)
            {
                return;
            }

            await _localDatabaseService.ImportDatabaseAsync(file.FullPath);
            UpdateBackupStatus($"Đã import database: {file.FileName}");
            OnPropertyChanged(nameof(DownloadsTitle));
        }
        catch (Exception ex)
        {
            UpdateBackupStatus($"Import thất bại: {ex.Message}");
        }
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

    private void UpdateBackupStatus(string value)
    {
        if (_backupStatusText == value)
        {
            return;
        }

        _backupStatusText = value;
        OnPropertyChanged(nameof(BackupStatus));
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
