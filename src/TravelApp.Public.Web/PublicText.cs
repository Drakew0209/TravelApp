using System.Globalization;

namespace TravelApp.Public.Web;

public static class PublicText
{
    public static string T(string vi, string ja, string de, string en = "")
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        return culture switch
        {
            var c when c.StartsWith("ja", StringComparison.OrdinalIgnoreCase) => ja,
            var c when c.StartsWith("de", StringComparison.OrdinalIgnoreCase) => de,
            var c when c.StartsWith("en", StringComparison.OrdinalIgnoreCase) => string.IsNullOrWhiteSpace(en) ? vi : en,
            _ => vi
        };
    }
}
