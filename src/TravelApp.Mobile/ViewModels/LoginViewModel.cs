using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _isPasswordHidden = true;
    private readonly IAuthApiClient _authApiClient;
    private readonly IProfileApiClient _profileApiClient;

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSignInEnabled));
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value) return;
            _password = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSignInEnabled));
        }
    }

    public bool IsPasswordHidden
    {
        get => _isPasswordHidden;
        set
        {
            if (_isPasswordHidden == value) return;
            _isPasswordHidden = value;
            OnPropertyChanged();
        }
    }

    public bool IsSignInEnabled =>
        !string.IsNullOrWhiteSpace(Email)
        && EmailRegex.IsMatch(Email.Trim())
        && !string.IsNullOrWhiteSpace(Password);

    public string PageTitle => AppStrings.LoginTitle;
    public string PageSubtitle => AppStrings.LoginSubtitle;
    public string CurrentLanguageText => $"{AppStrings.LanguagePrefix} {UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)}";
    public string EmailPlaceholder => AppStrings.EmailPlaceholder;
    public string PasswordPlaceholder => AppStrings.PasswordPlaceholder;
    public string ShowPasswordText => AppStrings.Show;
    public string HidePasswordText => AppStrings.Hide;
    public string SignInButtonText => AppStrings.SignIn;
    public string NoAccountYetText => AppStrings.NoAccountYet;
    public string CreateOneText => AppStrings.CreateOne;
    public ICommand BackCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand SignInCommand { get; }
    public ICommand RegisterCommand { get; }

    public LoginViewModel(IAuthApiClient authApiClient, IProfileApiClient profileApiClient)
    {
        _authApiClient = authApiClient;
        _profileApiClient = profileApiClient;
        UserProfileService.ProfileChanged += OnProfileChanged;
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        TogglePasswordVisibilityCommand = new Command(() => IsPasswordHidden = !IsPasswordHidden);
        RegisterCommand = new Command(async () => await Shell.Current.GoToAsync("RegisterPage"));
        SignInCommand = new Command(async () =>
        {
            if (!await ValidateInputAsync())
                return;

            var result = await _authApiClient.LoginAsync(new LoginRequestDto(Email.Trim(), Password));
            if (result is null)
            {
                if (Shell.Current is not null)
                    await Shell.Current.DisplayAlert(AppStrings.SignInFailedTitle, AppStrings.SignInFailedMessage, "OK");
                return;
            }

            UserProfileService.Reset();
            UserProfileService.ApplyAuthenticatedIdentity(result.UserId, Email.Trim(), result.FullName, result.Roles);
            AuthStateService.IsLoggedIn = true;

            await SyncProfileAsync();
            await Shell.Current.GoToAsync("..");
        });
    }

    private async Task SyncProfileAsync()
    {
        try
        {
            var profile = await _profileApiClient.GetMyProfileAsync();
            UserProfileService.ApplyProfile(profile);
        }
        catch
        {
        }
    }

    private async Task<bool> ValidateInputAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationEmailRequired, "OK");
            return false;
        }

        if (!EmailRegex.IsMatch(Email.Trim()))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationEmailInvalid, "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationPasswordRequired, "OK");
            return false;
        }

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged(nameof(EmailPlaceholder));
        OnPropertyChanged(nameof(PasswordPlaceholder));
        OnPropertyChanged(nameof(ShowPasswordText));
        OnPropertyChanged(nameof(HidePasswordText));
        OnPropertyChanged(nameof(SignInButtonText));
        OnPropertyChanged(nameof(NoAccountYetText));
        OnPropertyChanged(nameof(CreateOneText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
