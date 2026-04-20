using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TravelApp.Models.Contracts;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.ViewModels;

public class EditProfileViewModel : INotifyPropertyChanged
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private string _email = string.Empty;
    private string _fullName = string.Empty;
    private string _countryCode = string.Empty;
    private string _phoneNumber = string.Empty;
    private readonly IProfileApiClient _profileApiClient;

    private static bool IsVietnamese => UserProfileService.PreferredLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

    public string PageTitle => IsVietnamese ? "Chỉnh sửa hồ sơ" : "Edit Profile";
    public string CurrentLanguageText => $"{(IsVietnamese ? "Ngôn ngữ:" : "Language:")} {UserProfileService.GetLanguageDisplayText(UserProfileService.PreferredLanguage)}";
    public string EmailAddressLabel => IsVietnamese ? "Địa chỉ email" : "Email Address";
    public string FullNameLabel => IsVietnamese ? "Họ và tên" : "Full name";
    public string MobileNumberLabel => IsVietnamese ? "Số điện thoại" : "Mobile number";
    public string EmailPlaceholderText => IsVietnamese ? "ten@ban.com" : "your@email.com";
    public string FullNamePlaceholderText => IsVietnamese ? "Họ và tên của bạn" : "Your full name";
    public string PhonePlaceholderText => IsVietnamese ? "Số điện thoại" : "Phone number";
    public string UpdateProfileText => IsVietnamese ? "Cập nhật hồ sơ" : "Update Profile";
    public string DeleteAccountText => IsVietnamese ? "Xóa tài khoản" : "Delete account";
    public string SuccessTitle => IsVietnamese ? "Thành công" : "Success";
    public string ErrorTitle => IsVietnamese ? "Lỗi" : "Error";
    public string UpdateFailedMessage => IsVietnamese ? "Không thể cập nhật hồ sơ." : "Failed to update profile.";
    public string UpdatedMessage => IsVietnamese ? "Đã cập nhật hồ sơ." : "Profile updated.";
    public string DeleteConfirmTitle => IsVietnamese ? "Xóa tài khoản" : "Delete account";
    public string DeleteConfirmMessage => IsVietnamese ? "Bạn có chắc muốn xóa tài khoản này không?" : "Are you sure you want to delete this account?";
    public string DeleteConfirmAccept => IsVietnamese ? "Xóa" : "Delete";
    public string DeleteConfirmCancel => IsVietnamese ? "Hủy" : "Cancel";

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUpdateEnabled));
            OnPropertyChanged(nameof(UpdateButtonColor));
        }
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            if (_fullName == value) return;
            _fullName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUpdateEnabled));
            OnPropertyChanged(nameof(UpdateButtonColor));
        }
    }

    public string CountryCode
    {
        get => _countryCode;
        set
        {
            if (_countryCode == value) return;
            _countryCode = value;
            OnPropertyChanged();
        }
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (_phoneNumber == value) return;
            _phoneNumber = value;
            OnPropertyChanged();
        }
    }

    public bool IsUpdateEnabled =>
        !string.IsNullOrWhiteSpace(Email)
        && EmailRegex.IsMatch(Email.Trim())
        && !string.IsNullOrWhiteSpace(FullName);

    public Color UpdateButtonColor => IsUpdateEnabled ? Color.FromArgb("#E31667") : Color.FromArgb("#D7DCEA");

    public ICommand BackCommand { get; }
    public ICommand UpdateProfileCommand { get; }
    public ICommand DeleteAccountCommand { get; }

    public EditProfileViewModel(IProfileApiClient profileApiClient)
    {
        _profileApiClient = profileApiClient;
        Email = UserProfileService.Email;
        FullName = UserProfileService.FullName;
        CountryCode = UserProfileService.CountryCode;
        PhoneNumber = UserProfileService.PhoneNumber;

        UserProfileService.ProfileChanged += OnProfileChanged;

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        UpdateProfileCommand = new Command(async () => await UpdateProfileAsync());
        DeleteAccountCommand = new Command(async () => await DeleteAccountAsync());
    }

    private async Task UpdateProfileAsync()
    {
        if (!IsUpdateEnabled)
            return;

        var email = Email?.Trim() ?? string.Empty;
        var fullName = FullName?.Trim() ?? string.Empty;
        var countryCode = CountryCode?.Trim() ?? string.Empty;
        var phoneNumber = PhoneNumber?.Trim() ?? string.Empty;
        var preferredLanguage = string.IsNullOrWhiteSpace(UserProfileService.PreferredLanguage)
            ? "vi"
            : UserProfileService.PreferredLanguage.Trim();

        var request = new UpdateProfileRequestDto(
            email,
            fullName,
            countryCode,
            phoneNumber,
            preferredLanguage);

        var isSuccess = await _profileApiClient.UpdateMyProfileAsync(request);
        if (!isSuccess)
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert(ErrorTitle, UpdateFailedMessage, "OK");
            return;
        }

        UserProfileService.Email = request.Email;
        UserProfileService.FullName = request.FullName;
        UserProfileService.CountryCode = request.CountryCode;
        UserProfileService.PhoneNumber = request.PhoneNumber;

        if (Shell.Current is not null)
            await Shell.Current.DisplayAlert(SuccessTitle, UpdatedMessage, "OK");
    }

    private async Task DeleteAccountAsync()
    {
        if (Shell.Current is null)
            return;

        var confirm = await Shell.Current.DisplayAlert(DeleteConfirmTitle, DeleteConfirmMessage, DeleteConfirmAccept, DeleteConfirmCancel);
        if (!confirm)
            return;

        AuthStateService.IsLoggedIn = false;
        UserProfileService.Reset();
        await Shell.Current.GoToAsync("//ExplorePage");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnProfileChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(CurrentLanguageText));
        OnPropertyChanged(nameof(EmailAddressLabel));
        OnPropertyChanged(nameof(FullNameLabel));
        OnPropertyChanged(nameof(MobileNumberLabel));
        OnPropertyChanged(nameof(EmailPlaceholderText));
        OnPropertyChanged(nameof(FullNamePlaceholderText));
        OnPropertyChanged(nameof(PhonePlaceholderText));
        OnPropertyChanged(nameof(UpdateProfileText));
        OnPropertyChanged(nameof(DeleteAccountText));
        OnPropertyChanged(nameof(SuccessTitle));
        OnPropertyChanged(nameof(ErrorTitle));
        OnPropertyChanged(nameof(UpdateFailedMessage));
        OnPropertyChanged(nameof(UpdatedMessage));
        OnPropertyChanged(nameof(DeleteConfirmTitle));
        OnPropertyChanged(nameof(DeleteConfirmMessage));
        OnPropertyChanged(nameof(DeleteConfirmAccept));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        UserProfileService.ProfileChanged -= OnProfileChanged;
    }
}
