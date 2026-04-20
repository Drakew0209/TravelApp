using System.Globalization;

namespace TravelApp.Admin.Web.Models;

public static class AdminText
{
    public static string T(string fallback, string? ja = null, string? de = null)
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return lang switch
        {
            "ja" => string.IsNullOrWhiteSpace(ja) ? fallback : ja,
            "de" => string.IsNullOrWhiteSpace(de) ? fallback : de,
            _ => fallback
        };
    }

    public static string T(string vi, string en, string? ja, string? de)
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return lang switch
        {
            "en" => en,
            "ja" => string.IsNullOrWhiteSpace(ja) ? vi : ja,
            "de" => string.IsNullOrWhiteSpace(de) ? vi : de,
            _ => vi
        };
    }
}
