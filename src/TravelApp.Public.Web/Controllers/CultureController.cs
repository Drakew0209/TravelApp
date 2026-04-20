using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace TravelApp.Public.Web.Controllers;

[AllowAnonymous]
[Route("[controller]/[action]")]
public class CultureController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return LocalRedirect(string.IsNullOrWhiteSpace(redirectUri) ? Url.Content("~/") : redirectUri);
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(string.IsNullOrWhiteSpace(redirectUri) ? Url.Content("~/") : redirectUri);
    }
}
