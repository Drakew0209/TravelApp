using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TravelApp.Admin.Web.Services;

namespace TravelApp.Admin.Web.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class MediaController : Controller
{
    private readonly ITravelAppApiClient _apiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _environment;

    public MediaController(ITravelAppApiClient apiClient, IHttpClientFactory httpClientFactory, IWebHostEnvironment environment)
    {
        _apiClient = apiClient;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromQuery] string folder = "images", CancellationToken cancellationToken = default)
    {
        var url = await _apiClient.UploadImageAsync(file, folder, cancellationToken);
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new { message = "Không thể upload ảnh. Vui lòng thử lại." });
        }

        return Ok(new { url });
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Preview([FromQuery] string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return PlaceholderImage();
        }

        var client = _httpClientFactory.CreateClient();
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return PlaceholderImage();
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return File(bytes, contentType);
        }
        catch
        {
            return PlaceholderImage();
        }
    }

    private IActionResult PlaceholderImage()
    {
        var path = Path.Combine(_environment.WebRootPath ?? string.Empty, "img", "image-placeholder.png");
        if (System.IO.File.Exists(path))
        {
            return PhysicalFile(path, "image/png");
        }

        return Redirect("https://placehold.co/800x600/png?text=No+Image");
    }
}
