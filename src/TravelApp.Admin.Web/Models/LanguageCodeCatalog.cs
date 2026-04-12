using Microsoft.AspNetCore.Mvc.Rendering;

namespace TravelApp.Admin.Web.Models;

public static class LanguageCodeCatalog
{
    public static List<SelectListItem> Create()
    {
        return new List<SelectListItem>
        {
            new("Tiếng Việt (vi)", "vi"),
            new("English (en)", "en"),
            new("日本語 (ja)", "ja"),
            new("한국어 (ko)", "ko"),
            new("中文 (zh)", "zh"),
            new("Français (fr)", "fr"),
            new("Deutsch (de)", "de"),
            new("Español (es)", "es"),
            new("Italiano (it)", "it"),
            new("Русский (ru)", "ru"),
            new("ไทย (th)", "th"),
            new("العربية (ar)", "ar"),
            new("Português (pt)", "pt")
        };
    }
}
