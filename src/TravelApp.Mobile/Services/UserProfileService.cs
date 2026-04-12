using System.Globalization;

namespace TravelApp.Services;

public static class UserProfileService
{
    private static readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
    private static string _email = string.Empty;
    private static string _fullName = string.Empty;
    private static string _phoneNumber = string.Empty;
    private static string _countryCode = string.Empty;
    private static string _preferredLanguage = string.Empty;

    public static string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string FullName
    {
        get => _fullName;
        set
        {
            if (_fullName == value) return;
            _fullName = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (_phoneNumber == value) return;
            _phoneNumber = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string CountryCode
    {
        get => _countryCode;
        set
        {
            if (_countryCode == value) return;
            _countryCode = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static string PreferredLanguage
    {
        get => string.IsNullOrWhiteSpace(_preferredLanguage) ? GetFallbackPreferredLanguage() : _preferredLanguage;
        set
        {
            if (_preferredLanguage == value) return;
            _preferredLanguage = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static IReadOnlyCollection<string> Roles => _roles.ToArray();

    public static bool CanEditSpeechText => HasAnyRole("owner", "admin", "superadmin");

    public static void SetRoles(IEnumerable<string>? roles)
    {
        _roles.Clear();

        foreach (var role in roles ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                _roles.Add(role.Trim());
            }
        }

        ProfileChanged?.Invoke(null, EventArgs.Empty);
    }

    public static bool HasRole(string roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) && _roles.Contains(roleName.Trim());
    }

    public static bool HasAnyRole(params string[] roleNames)
    {
        return roleNames.Any(HasRole);
    }

    public static event EventHandler? ProfileChanged;

    private static string GetFallbackPreferredLanguage()
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(language) || string.Equals(language, "iv", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : language;
    }
}
