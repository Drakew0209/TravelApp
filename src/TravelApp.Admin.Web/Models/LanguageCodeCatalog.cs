using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using TravelApp.Application.Utilities;

namespace TravelApp.Admin.Web.Models;

public static class LanguageCodeCatalog
{
    private static readonly (string Code, string Label)[] PreferredLanguages =
    [
        ("vi-VN", "Tiếng Việt (vi-VN)"),
        ("en-US", "English (en-US)"),
        ("ja-JP", "日本語 (ja-JP)"),
        ("de-DE", "Deutsch (de-DE)")
    ];

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);
    private static readonly object CacheLock = new();
    private static IReadOnlyList<SelectListItem>? _cachedItems;
    private static DateTimeOffset _cachedAtUtc;

    public static List<SelectListItem> Create()
    {
        lock (CacheLock)
        {
            if (_cachedItems is not null && DateTimeOffset.UtcNow - _cachedAtUtc < CacheDuration)
            {
                return _cachedItems.ToList();
            }

            _cachedItems = BuildItems();
            _cachedAtUtc = DateTimeOffset.UtcNow;
            return _cachedItems.ToList();
        }
    }

    private static IReadOnlyList<SelectListItem> BuildItems()
    {
        return PreferredLanguages
            .Select(x => new SelectListItem(x.Label, x.Code))
            .ToList();
    }

    private static string BuildLabel(CultureInfo culture, string code)
    {
        var name = string.IsNullOrWhiteSpace(culture.NativeName)
            ? culture.EnglishName
            : culture.NativeName;

        return string.IsNullOrWhiteSpace(name)
            ? code.ToUpperInvariant()
            : $"{name} ({code})";
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
    }
}
