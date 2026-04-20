using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Resources.Strings;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public sealed class RegisterViewModel : INotifyPropertyChanged
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private readonly IAuthApiClient _authApiClient;
    private readonly IProfileApiClient _profileApiClient;

    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isPasswordHidden = true;

    public string FullName
    {
        get => _fullName;
        set
        {
            if (_fullName == value) return;
            _fullName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRegisterEnabled));
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRegisterEnabled));
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
            OnPropertyChanged(nameof(IsRegisterEnabled));
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (_confirmPassword == value) return;
            _confirmPassword = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRegisterEnabled));
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

    public bool IsRegisterEnabled =>
        !string.IsNullOrWhiteSpace(FullName)
        && !string.IsNullOrWhiteSpace(Email)
        && EmailRegex.IsMatch(Email.Trim())
        && !string.IsNullOrWhiteSpace(Password)
        && string.Equals(Password, ConfirmPassword, StringComparison.Ordinal);

    public string PageTitle => AppStrings.RegisterTitle;
    public string PageSubtitle => AppStrings.RegisterSubtitle;
    public string CurrentLanguageText => $"{AppStrings.LanguagePrefix} {UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)}";
    public string FullNamePlaceholder => AppStrings.FullNamePlaceholder;
    public string EmailPlaceholder => AppStrings.EmailPlaceholder;
    public string PasswordPlaceholder => AppStrings.PasswordPlaceholder;
    public string ConfirmPasswordPlaceholder => AppStrings.ConfirmPasswordPlaceholder;
    public string ShowPasswordText => AppStrings.Show;
    public string HidePasswordText => AppStrings.Hide;
    public string RegisterButtonText => AppStrings.CreateAccount;
    public string AlreadyHaveAccountText => AppStrings.AlreadyHaveAccount;
    public string SignInText => AppStrings.SignIn;

    public ICommand BackCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand RegisterCommand { get; }

    public RegisterViewModel(IAuthApiClient authApiClient, IProfileApiClient profileApiClient)
    {
        _authApiClient = authApiClient;
        _profileApiClient = profileApiClient;
        UserProfileService.ProfileChanged += OnProfileChanged;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        TogglePasswordVisibilityCommand = new Command(() => IsPasswordHidden = !IsPasswordHidden);
        RegisterCommand = new Command(async () => await RegisterAsync());
    }

    private async Task RegisterAsync()
    {
        if (!await ValidateInputAsync())
        {
            return;
        }

        try
        {
            var result = await _authApiClient.RegisterAsync(new RegisterRequestDto(Email.Trim(), Password, FullName.Trim()));
            if (result is null)
            {
                await Shell.Current.DisplayAlert(AppStrings.RegisterFailedTitle, AppStrings.RegisterFailedMessage, "OK");
                return;
            }

            UserProfileService.Reset();
            UserProfileService.ApplyAuthenticatedIdentity(result.UserId, Email.Trim(), string.IsNullOrWhiteSpace(result.FullName) ? FullName.Trim() : result.FullName, result.Roles);
            AuthStateService.IsLoggedIn = true;

            await SyncProfileAsync();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppStrings.RegisterFailedTitle, ex.Message, "OK");
        }
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
        if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
        {
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationFullName, "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationEmailRequired, "OK");
            return false;
        }

        if (!EmailRegex.IsMatch(Email.Trim()))
        {
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationEmailInvalid, "OK");
            return false;
        }

        if (!IsStrongPassword(Password))
        {
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationPasswordStrength, "OK");
            return false;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            await Shell.Current.DisplayAlert(AppStrings.ValidationTitle, AppStrings.ValidationPasswordMismatch, "OK");
            return false;
        }

        return true;
    }

    private static bool IsStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        return password.Any(char.IsUpper)
               && password.Any(char.IsLower)
               && password.Any(char.IsDigit)
               && password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged(nameof(FullNamePlaceholder));
        OnPropertyChanged(nameof(EmailPlaceholder));
        OnPropertyChanged(nameof(PasswordPlaceholder));
        OnPropertyChanged(nameof(ConfirmPasswordPlaceholder));
        OnPropertyChanged(nameof(ShowPasswordText));
        OnPropertyChanged(nameof(HidePasswordText));
        OnPropertyChanged(nameof(RegisterButtonText));
        OnPropertyChanged(nameof(AlreadyHaveAccountText));
        OnPropertyChanged(nameof(SignInText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
