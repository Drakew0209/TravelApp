using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Resources.Strings;
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
    public string PageTitle => AppStrings.ProfileTitle;

    public string GreetingTitle => IsLoggedIn
        ? string.IsNullOrWhiteSpace(UserProfileService.FullName)
            ? AppStrings.WelcomeCardTitle
            : string.Format(CultureInfo.CurrentUICulture, AppStrings.GreetingLoggedInFormat, UserProfileService.FullName)
        : AppStrings.WelcomeCardTitle;

    public string GreetingSubtitle => IsLoggedIn
        ? string.IsNullOrWhiteSpace(UserProfileService.Email)
            ? AppStrings.YourAccountIsReady
            : UserProfileService.Email
        : AppStrings.SignInToManageDownloadsBookmarksAndYourProfile;
    public string PrimaryActionText => IsLoggedIn ? AppStrings.SignOut : AppStrings.SignIn;

    public bool ShowAccountSection => IsLoggedIn;
    public bool ShowPurchases => IsLoggedIn;
    public bool ShowDownloads => IsLoggedIn;
    public bool ShowBookmarks => IsLoggedIn;
    public string DownloadsTitle => _offlineDownloadsCount > 0 ? $"{AppStrings.Downloads} ({_offlineDownloadsCount})" : AppStrings.Downloads;
    public string BackupStatus => _backupStatusText;
    public string CurrentLanguageText => $"{AppStrings.LanguagePrefix} {UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)}";
    public string LanguageDisplayText => UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage);
    public string BookmarksTitle => AppStrings.Bookmarks;
    public string MyAccountTitle => AppStrings.MyAccount;
    public string EditProfileText => AppStrings.EditProfile;
    public string PreferencesTitle => AppStrings.Preferences;
    public string LanguageLabel => AppStrings.Language;
    public string UserPreferencesText => AppStrings.UserPreferences;
    public string LanUrlSettingsText => AppStrings.LanUrlSettings;
    public string BackupAndRestoreTitle => AppStrings.BackupAndRestore;
    public string BackupDescriptionText => AppStrings.BackupDescription;
    public string ExportDatabaseText => AppStrings.ExportDatabase;
    public string ImportDatabaseText => AppStrings.ImportDatabase;
    public string LanguageHelpText => AppStrings.LanguageHelp;

    public ICommand BackCommand { get; }
    public ICommand PrimaryActionCommand { get; }
    public ICommand OpenEditProfileCommand { get; }
    public ICommand OpenNetworkSettingsCommand { get; }
    public ICommand OpenDownloadsCommand { get; }
    public ICommand OpenBookmarksCommand { get; }
    public ICommand ExportDatabaseCommand { get; }
    public ICommand ImportDatabaseCommand { get; }
    public ICommand ChangeLanguageCommand { get; }

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
        OpenNetworkSettingsCommand = new Command(async () => await Shell.Current.GoToAsync("NetworkSettingsPage"));
        OpenDownloadsCommand = new Command(async () => await Shell.Current.GoToAsync("MyAudioLibraryPage"));
        OpenBookmarksCommand = new Command(async () => await Shell.Current.GoToAsync("BookmarksHistoryPage?tab=bookmarks"));
        ExportDatabaseCommand = new Command(async () => await ExportDatabaseAsync());
        ImportDatabaseCommand = new Command(async () => await ImportDatabaseAsync());
        ChangeLanguageCommand = new Command(async () => await ChangeLanguageAsync());
        PrimaryActionCommand = new Command(async () =>
        {
            if (IsLoggedIn)
            {
                await _authApiClient.LogoutAsync();
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
        _ = RefreshOfflineDownloadsCountAsync();

        if (IsLoggedIn)
        {
            _ = LoadProfileAsync();
        }
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        RaiseAuthBoundProperties();
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged(nameof(GreetingTitle));
        OnPropertyChanged(nameof(GreetingSubtitle));
        OnPropertyChanged(nameof(LanguageDisplayText));
        _ = RefreshOfflineDownloadsCountAsync();
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
        OnPropertyChanged(nameof(BookmarksTitle));
        OnPropertyChanged(nameof(MyAccountTitle));
        OnPropertyChanged(nameof(EditProfileText));
        OnPropertyChanged(nameof(PreferencesTitle));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(UserPreferencesText));
        OnPropertyChanged(nameof(LanUrlSettingsText));
        OnPropertyChanged(nameof(BackupAndRestoreTitle));
        OnPropertyChanged(nameof(BackupDescriptionText));
        OnPropertyChanged(nameof(ExportDatabaseText));
        OnPropertyChanged(nameof(ImportDatabaseText));
        OnPropertyChanged(nameof(LanguageHelpText));
        OnPropertyChanged(nameof(LanguageDisplayText));
        OnPropertyChanged(nameof(CurrentLanguageText));
    }

    private async Task ExportDatabaseAsync()
    {
        try
        {
            var exportDirectory = Path.Combine(FileSystem.CacheDirectory, "travelapp-backup");
            var exportedPath = await _localDatabaseService.ExportDatabaseAsync(exportDirectory);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppStrings.ExportDatabase,
                File = new ShareFile(exportedPath)
            });

            UpdateBackupStatus(string.Format(CultureInfo.CurrentUICulture, AppStrings.BackupExportSuccess, Path.GetFileName(exportedPath)));
        }
        catch (Exception ex)
        {
            UpdateBackupStatus(string.Format(CultureInfo.CurrentUICulture, AppStrings.BackupExportFailed, ex.Message));
        }
    }

    private async Task ImportDatabaseAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = AppStrings.ImportDatabase
            });

            if (file is null)
            {
                return;
            }

            await _localDatabaseService.ImportDatabaseAsync(file.FullPath);
            UpdateBackupStatus(string.Format(CultureInfo.CurrentUICulture, AppStrings.BackupImportSuccess, file.FileName));
            OnPropertyChanged(nameof(DownloadsTitle));
        }
        catch (Exception ex)
        {
            UpdateBackupStatus(string.Format(CultureInfo.CurrentUICulture, AppStrings.BackupImportFailed, ex.Message));
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

            UserProfileService.ApplyProfile(profile);
        }
        catch
        {
        }
    }

    private async Task ChangeLanguageAsync()
    {
        if (Shell.Current is null)
        {
            return;
        }

        var options = UserProfileService.SupportedLanguages
            .Select(code => (Code: code, Label: UserProfileService.GetLanguageDisplayText(code)))
            .ToList();

        var selection = await Shell.Current.DisplayActionSheet(AppStrings.ChooseAppLanguage, AppStrings.Cancel, null, options.Select(x => x.Label).ToArray());
        if (string.IsNullOrWhiteSpace(selection) || selection == AppStrings.Cancel)
        {
            return;
        }

        var selected = options.FirstOrDefault(x => string.Equals(x.Label, selection, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(selected.Code))
        {
            return;
        }

        UserProfileService.PreferredLanguage = selected.Code;

        if (IsLoggedIn)
        {
            try
            {
                var request = new UpdateProfileRequestDto(
                    UserProfileService.Email,
                    UserProfileService.FullName,
                    UserProfileService.CountryCode,
                    UserProfileService.PhoneNumber,
                    UserProfileService.PreferredLanguage);

                await _profileApiClient.UpdateMyProfileAsync(request);
            }
            catch
            {
            }
        }

        OnPropertyChanged(nameof(LanguageDisplayText));
        OnPropertyChanged(nameof(CurrentLanguageText));
        await Shell.Current.DisplayAlert(AppStrings.LanguageUpdatedTitle, string.Format(CultureInfo.CurrentUICulture, AppStrings.LanguageUpdatedMessage, UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)), "OK");
    }

    public void Dispose()
    {
        AuthStateService.AuthStateChanged -= OnAuthStateChanged;
        UserProfileService.ProfileChanged -= OnProfileChanged;
    }
}
