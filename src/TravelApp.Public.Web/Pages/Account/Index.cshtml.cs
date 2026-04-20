using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelApp.Public.Web;

namespace TravelApp.Public.Web.Pages.Account;

[Authorize]
public sealed class IndexModel : PageModel
{
    public string PageTitle => PublicText.T("Tài khoản", "アカウント", "Konto", "Account");
    public string? DisplayName => User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name;
    public string? Email => User.FindFirstValue(ClaimTypes.Email);
    public IReadOnlyList<string> Roles => User.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public void OnGet()
    {
    }
}
