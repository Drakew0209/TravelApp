using System.Globalization;
using TravelApp.Models.Contracts;

namespace TravelApp.Services;

public static class UserProfileService
{
    private static readonly string[] SupportedLanguageCodes =
    [
        "vi-VN",
        "en-US",
        "ja-JP",
        "de-DE"
    ];

    private static readonly Dictionary<string, string> SupportedLanguageAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = "vi-VN",
        ["vi-vn"] = "vi-VN",
        ["en"] = "en-US",
        ["en-us"] = "en-US",
        ["en-gb"] = "en-US",
        ["ja"] = "ja-JP",
        ["ja-jp"] = "ja-JP",
        ["de"] = "de-DE",
        ["de-de"] = "de-DE"
    };

    private static readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
    private static string _userId = string.Empty;
    private static string _email = string.Empty;
    private static string _fullName = string.Empty;
    private static string _phoneNumber = string.Empty;
    private static string _countryCode = string.Empty;
    private static string _preferredLanguage = string.Empty;

    static UserProfileService()
    {
        ApplyLanguageCulture(GetFallbackPreferredLanguage());
    }

    public static string UserId
    {
        get => _userId;
        set
        {
            if (_userId == value) return;
            _userId = value;
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

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
            var normalized = NormalizePreferredLanguage(value);
            if (_preferredLanguage == normalized) return;

            _preferredLanguage = normalized;
            ApplyLanguageCulture(_preferredLanguage);
            ProfileChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static IReadOnlyList<string> SupportedLanguages => SupportedLanguageCodes;

    public static IReadOnlyCollection<string> Roles => _roles.ToArray();

    public static bool IsGuest => _roles.Count == 0;

    public static bool IsUser => HasAnyRole("user", "owner", "admin", "superadmin");

    public static bool IsAdmin => HasAnyRole("admin", "superadmin");

    public static bool IsOwner => HasAnyRole("owner", "admin", "superadmin");

    public static bool CanEditSpeechText => IsOwner;

    public static void ApplyAuthenticatedIdentity(string? userId, string email, string? fullName, IEnumerable<string>? roles)
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim();
        Email = email.Trim();
        FullName = string.IsNullOrWhiteSpace(fullName) ? string.Empty : fullName.Trim();
        SetRoles(roles);
    }

    public static void ApplyProfile(ProfileDto? profile)
    {
        if (profile is null)
        {
            return;
        }

        Email = profile.Email?.Trim() ?? string.Empty;
        FullName = profile.FullName?.Trim() ?? string.Empty;
        CountryCode = profile.CountryCode?.Trim() ?? string.Empty;
        PhoneNumber = profile.PhoneNumber?.Trim() ?? string.Empty;
        PreferredLanguage = profile.PreferredLanguage?.Trim() ?? string.Empty;
    }

    public static void Reset()
    {
        _roles.Clear();
        _userId = string.Empty;
        _email = string.Empty;
        _fullName = string.Empty;
        _phoneNumber = string.Empty;
        _countryCode = string.Empty;
        _preferredLanguage = string.Empty;
        ApplyLanguageCulture(GetFallbackPreferredLanguage());
        ProfileChanged?.Invoke(null, EventArgs.Empty);
    }

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

    public static string GetLanguageDisplayText(string? languageCode)
    {
        var normalized = NormalizePreferredLanguage(languageCode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "--";
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            return string.IsNullOrWhiteSpace(culture.NativeName)
                ? normalized.ToUpperInvariant()
                : $"{culture.NativeName} ({normalized.ToUpperInvariant()})";
        }
        catch
        {
            return normalized.ToUpperInvariant();
        }
    }

    public static void ApplyPreferredLanguageCulture()
    {
        ApplyLanguageCulture(PreferredLanguage);
    }

    public static event EventHandler? ProfileChanged;

    private static string GetFallbackPreferredLanguage()
    {
        return NormalizeToSupportedLanguage(LanguageCodeNormalizer.NormalizeToLocaleCode(CultureInfo.CurrentUICulture.Name)) ?? "en-US";
    }

    private static string NormalizePreferredLanguage(string? languageCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
        return NormalizeToSupportedLanguage(normalized) ?? GetFallbackPreferredLanguage();
    }

    private static string? NormalizeToSupportedLanguage(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var normalized = languageCode.Trim();
        if (SupportedLanguageCodes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            return SupportedLanguageCodes.First(code => string.Equals(code, normalized, StringComparison.OrdinalIgnoreCase));
        }

        if (SupportedLanguageAliases.TryGetValue(normalized, out var alias))
        {
            return alias;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            return SupportedLanguageAliases.TryGetValue(culture.TwoLetterISOLanguageName, out alias)
                ? alias
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyLanguageCulture(string? languageCode)
    {
        var cultureCode = NormalizePreferredLanguage(languageCode);
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch
        {
            // ignored: fall back to current runtime culture
        }
    }
}
