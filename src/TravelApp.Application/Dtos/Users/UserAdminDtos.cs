namespace TravelApp.Application.Dtos.Users;

public sealed class UserAdminDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public IReadOnlyList<RoleAdminDto> Roles { get; set; } = [];
}

public sealed class RoleAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpsertUserRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> RoleIds { get; set; } = [];
}
