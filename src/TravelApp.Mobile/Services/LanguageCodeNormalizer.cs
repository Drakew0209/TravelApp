using TravelApp.Application.Utilities;

namespace TravelApp.Services;

public static class LanguageCodeNormalizer
{
    public static string NormalizeToLocaleCode(string? languageCode)
    {
        return TravelApp.Application.Utilities.LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
    }
}
