using Microsoft.AspNetCore.Mvc;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public MediaController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromQuery] string folder = "images", CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return BadRequest(new { message = "Invalid file name." });
        }

        var normalizedFolder = string.IsNullOrWhiteSpace(folder) ? "images" : folder.Trim().ToLowerInvariant();
        var uploadsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads", normalizedFolder);
        Directory.CreateDirectory(uploadsRoot);

        var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsRoot, safeFileName);

        await using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var publicUrl = $"{Request.Scheme}://{Request.Host}/uploads/{normalizedFolder}/{safeFileName}";
        return Ok(new { url = publicUrl });
    }
}
