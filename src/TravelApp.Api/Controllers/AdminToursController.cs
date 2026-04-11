using Microsoft.AspNetCore.Mvc;
using TravelApp.Application.Abstractions.Tours;
using TravelApp.Application.Dtos.Tours;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/admin/tours")]
public class AdminToursController : ControllerBase
{
    private readonly ITourAdminService _tourAdminService;

    public AdminToursController(ITourAdminService tourAdminService)
    {
        _tourAdminService = tourAdminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TourAdminDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _tourAdminService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TourAdminDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _tourAdminService.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TourAdminDto>> Create([FromBody] UpsertTourRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tourAdminService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertTourRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _tourAdminService.UpdateAsync(id, request, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _tourAdminService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
