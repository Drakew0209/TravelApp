using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelApp.Public.Web.Services;

namespace TravelApp.Public.Web.Pages.Auth;

public sealed class LogoutModel : PageModel
{
    private readonly IPublicAuthApiClient _authApiClient;

    public LogoutModel(IPublicAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var refreshToken = AuthSessionHelper.GetRefreshToken(authResult.Properties);

        await _authApiClient.LogoutAsync(refreshToken, cancellationToken);
        await AuthSessionHelper.SignOutAsync(HttpContext);

        return LocalRedirect(Url.Content("~/"));
    }
}
