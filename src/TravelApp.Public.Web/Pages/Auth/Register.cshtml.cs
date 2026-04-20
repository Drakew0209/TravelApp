using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelApp.Public.Web;
using TravelApp.Application.Abstractions.Auth;
using TravelApp.Public.Web.Services;

namespace TravelApp.Public.Web.Pages.Auth;

public sealed class RegisterModel : PageModel
{
    private readonly IPublicAuthApiClient _authApiClient;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public RegisterModel(IPublicAuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SafeReturnUrl());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _authApiClient.RegisterAsync(Input.Email.Trim(), Input.Password, Input.FullName.Trim(), CancellationToken.None);
            if (result is null)
            {
                ModelState.AddModelError(string.Empty, PublicText.T("Không thể tạo tài khoản.", "アカウントを作成できませんでした。", "Das Konto konnte nicht erstellt werden.", "Unable to create your account."));
                return Page();
            }

            await AuthSessionHelper.SignInAsync(HttpContext, result, Input.Email.Trim(), Input.FullName.Trim());
            return LocalRedirect(SafeReturnUrl());
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (OperationCanceledException)
        {
            ModelState.AddModelError(string.Empty, PublicText.T("Không thể tạo tài khoản lúc này.", "現在アカウントを作成できません。", "Das Konto kann derzeit nicht erstellt werden.", "Unable to create your account right now."));
            return Page();
        }
    }

    private string SafeReturnUrl()
    {
        return !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? ReturnUrl
            : Url.Page("/Account/Index") ?? Url.Content("~/");
    }

    public sealed class InputModel
    {
        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(200, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
