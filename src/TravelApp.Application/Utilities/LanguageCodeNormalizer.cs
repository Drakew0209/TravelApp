using System.Globalization;

namespace TravelApp.Application.Utilities;

public static class LanguageCodeNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> PreferredLocaleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = "vi-VN",
        ["en"] = "en-US",
        ["ja"] = "ja-JP",
        ["ko"] = "ko-KR",
        ["zh"] = "zh-CN",
        ["fr"] = "fr-FR",
        ["de"] = "de-DE",
        ["es"] = "es-ES",
        ["it"] = "it-IT",
        ["ru"] = "ru-RU",
        ["th"] = "th-TH",
        ["ar"] = "ar-SA",
        ["pt"] = "pt-BR"
    };

    public static string NormalizeToLocaleCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return string.Empty;
        }

        var candidate = languageCode.Trim().Replace('_', '-');
        if (candidate.Length is < 2 or > 15)
        {
            return string.Empty;
        }

        if (PreferredLocaleMap.TryGetValue(candidate, out var preferred))
        {
            return preferred;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(candidate);
            var name = culture.Name.Trim();
            return string.IsNullOrWhiteSpace(name) ? string.Empty : name;
        }
        catch
        {
            return string.Empty;
        }
    }
}
