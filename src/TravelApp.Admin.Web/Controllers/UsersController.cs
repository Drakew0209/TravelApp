using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TravelApp.Admin.Web.Models.Users;
using TravelApp.Admin.Web.Services;
using TravelApp.Application.Dtos.Users;

namespace TravelApp.Admin.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly ITravelAppApiClient _apiClient;

    public UsersController(ITravelAppApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _apiClient.GetUsersAsync(cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await BuildEditorModelAsync(null, null, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserEditorViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required for new user.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildEditorModelAsync(model, null, cancellationToken));
        }

        var created = await _apiClient.CreateUserAsync(ToRequest(model), cancellationToken);
        if (created is null)
        {
            ModelState.AddModelError(string.Empty, "Username, email or password is invalid.");
            return View(await BuildEditorModelAsync(model, null, cancellationToken));
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _apiClient.GetUserAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        return View(await BuildEditorModelAsync(null, existing, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var existing = await _apiClient.GetUserAsync(id, cancellationToken);
            return View(await BuildEditorModelAsync(model, existing, cancellationToken));
        }

        var updated = await _apiClient.UpdateUserAsync(id, ToRequest(model), cancellationToken);
        if (!updated)
        {
            var existing = await _apiClient.GetUserAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, "Username or email is already in use.");
            return View(await BuildEditorModelAsync(model, existing, cancellationToken));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _apiClient.DeleteUserAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserEditorViewModel> BuildEditorModelAsync(UserEditorViewModel? source, UserAdminDto? existing, CancellationToken cancellationToken)
    {
        var roles = await _apiClient.GetRolesAsync(cancellationToken);
        var selectedRoleIds = source?.SelectedRoleIds?.ToList()
            ?? existing?.Roles.Select(x => x.Id).ToList()
            ?? roles.Where(x => string.Equals(x.Name, "User", StringComparison.OrdinalIgnoreCase)).Select(x => x.Id).ToList();

        var model = source ?? new UserEditorViewModel();
        model.Id = existing?.Id ?? model.Id;
        model.UserName = source?.UserName ?? existing?.UserName ?? string.Empty;
        model.Email = source?.Email ?? existing?.Email ?? string.Empty;
        model.IsActive = source?.Id is not null ? source.IsActive : existing?.IsActive ?? true;
        model.AvailableRoles = roles.Select(x => new SelectListItem
        {
            Text = x.Name,
            Value = x.Id.ToString(),
            Selected = selectedRoleIds.Contains(x.Id)
        }).ToList();
        model.SelectedRoleIds = selectedRoleIds;
        return model;
    }

    private static UpsertUserRequestDto ToRequest(UserEditorViewModel model)
    {
        return new UpsertUserRequestDto
        {
            UserName = model.UserName,
            Email = model.Email,
            Password = string.IsNullOrWhiteSpace(model.Password) ? null : model.Password,
            IsActive = model.IsActive,
            RoleIds = model.SelectedRoleIds
        };
    }
}
