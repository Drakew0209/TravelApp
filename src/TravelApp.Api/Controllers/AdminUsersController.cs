using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Abstractions.Users;
using TravelApp.Application.Dtos.Users;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;

    public AdminUsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAdminDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _userAdminService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserAdminDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userAdminService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleAdminDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _userAdminService.GetRolesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserAdminDto>> Create([FromBody] UpsertUserRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userAdminService.CreateAsync(request, cancellationToken);
        return result is null ? Conflict(new { message = "Username, email or password is invalid" }) : CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertUserRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await _userAdminService.UpdateAsync(id, request, cancellationToken);
        if (updated)
        {
            return NoContent();
        }

        var existing = await _userAdminService.GetByIdAsync(id, cancellationToken);
        return existing is null ? NotFound() : Conflict(new { message = "Username or email is already in use" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _userAdminService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
