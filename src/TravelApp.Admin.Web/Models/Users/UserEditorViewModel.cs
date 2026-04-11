using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TravelApp.Admin.Web.Models.Users;

public sealed class UserEditorViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;

    public List<int> SelectedRoleIds { get; set; } = [];

    public IReadOnlyList<SelectListItem> AvailableRoles { get; set; } = [];
}
