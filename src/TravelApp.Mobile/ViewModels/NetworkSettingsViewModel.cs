using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class NetworkSettingsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IEndpointSettingsService _endpointSettingsService;
    private readonly ILanEndpointDiscoveryService _lanEndpointDiscoveryService;
    private string _apiBaseUrl = string.Empty;
    private string _publicWebBaseUrl = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isDiscovering;

    public string PageTitle => AppStrings.NetworkSettingsTitle;
    public string DescriptionText => AppStrings.NetworkSettingsDescription;
    public string AutoFillButtonText => AppStrings.AutoFillFromDevMachine;
    public string ApiBaseUrlLabel => AppStrings.ApiBaseUrlLabel;
    public string PublicWebBaseUrlLabel => AppStrings.PublicWebBaseUrlLabel;
    public string HintText => AppStrings.NetworkSettingsHint;
    public string ResetButtonText => AppStrings.RestoreDefaults;
    public string SaveButtonText => AppStrings.Save;
    public string CurrentLanguageText => $"{AppStrings.LanguagePrefix} {UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)}";

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set
        {
            if (_apiBaseUrl == value)
            {
                return;
            }

            _apiBaseUrl = value;
            OnPropertyChanged();
        }
    }

    public string PublicWebBaseUrl
    {
        get => _publicWebBaseUrl;
        set
        {
            if (_publicWebBaseUrl == value)
            {
                return;
            }

            _publicWebBaseUrl = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsDiscovering
    {
        get => _isDiscovering;
        private set
        {
            if (_isDiscovering == value)
            {
                return;
            }

            _isDiscovering = value;
            OnPropertyChanged();
        }
    }

    public ICommand BackCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand AutoFillFromDevMachineCommand { get; }

    public NetworkSettingsViewModel(IEndpointSettingsService endpointSettingsService, ILanEndpointDiscoveryService lanEndpointDiscoveryService)
    {
        _endpointSettingsService = endpointSettingsService;
        _lanEndpointDiscoveryService = lanEndpointDiscoveryService;
        _endpointSettingsService.SettingsChanged += OnSettingsChanged;
        UserProfileService.ProfileChanged += OnProfileChanged;

        LoadFromService();

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        SaveCommand = new Command(Save);
        ResetCommand = new Command(ResetToDefaults);
        AutoFillFromDevMachineCommand = new Command(async () => await AutoFillFromDevMachineAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Save()
    {
        if (!TryValidateUrls(ApiBaseUrl, PublicWebBaseUrl, out var normalizedApi, out var normalizedPublic, out var error))
        {
            StatusMessage = error;
            return;
        }

        _endpointSettingsService.Update(normalizedApi, normalizedPublic);
        LoadFromService();
        StatusMessage = AppStrings.NetworkSettingsSaved;
    }

    private void ResetToDefaults()
    {
        _endpointSettingsService.ResetToDefaults();
        LoadFromService();
        StatusMessage = AppStrings.NetworkSettingsReset;
    }

    private async Task AutoFillFromDevMachineAsync()
    {
        if (IsDiscovering)
        {
            return;
        }

        IsDiscovering = true;
        StatusMessage = AppStrings.NetworkSettingsAutoFillPrompt;

        try
        {
            var result = await _lanEndpointDiscoveryService.TryDiscoverAsync();
            if (result is null)
            {
                StatusMessage = AppStrings.NetworkSettingsAutoFillNotFound;
                return;
            }

            ApiBaseUrl = result.ApiBaseUrl;
            PublicWebBaseUrl = result.PublicWebBaseUrl;
            _endpointSettingsService.Update(result.ApiBaseUrl, result.PublicWebBaseUrl);
            LoadFromService();
            StatusMessage = string.Format(CultureInfo.CurrentUICulture, AppStrings.NetworkSettingsAutoFillSuccessFormat, result.HostIpAddress);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentUICulture, AppStrings.NetworkSettingsAutoFillFailedFormat, ex.Message);
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    private void LoadFromService()
    {
        ApiBaseUrl = _endpointSettingsService.ApiBaseUrl;
        PublicWebBaseUrl = _endpointSettingsService.PublicWebBaseUrl;
    }

    private static bool TryValidateUrls(string? apiBaseUrl, string? publicWebBaseUrl, out string normalizedApi, out string normalizedPublic, out string error)
    {
        normalizedApi = string.Empty;
        normalizedPublic = string.Empty;
        error = string.Empty;

        if (!TryNormalizeAbsoluteUrl(apiBaseUrl, out normalizedApi))
        {
            error = AppStrings.NetworkSettingsInvalidApiUrl;
            return false;
        }

        if (!TryNormalizeAbsoluteUrl(publicWebBaseUrl, out normalizedPublic))
        {
            error = AppStrings.NetworkSettingsInvalidPublicUrl;
            return false;
        }

        return true;
    }

    private static bool TryNormalizeAbsoluteUrl(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };

        normalized = builder.Uri.ToString();
        return builder.Uri.Scheme is "http" or "https";
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(LoadFromService);
    }

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(DescriptionText));
        OnPropertyChanged(nameof(AutoFillButtonText));
        OnPropertyChanged(nameof(ApiBaseUrlLabel));
        OnPropertyChanged(nameof(PublicWebBaseUrlLabel));
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(ResetButtonText));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(CurrentLanguageText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _endpointSettingsService.SettingsChanged -= OnSettingsChanged;
        UserProfileService.ProfileChanged -= OnProfileChanged;
    }
}
