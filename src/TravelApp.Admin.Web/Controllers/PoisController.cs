using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Admin.Web.Models.Pois;
using TravelApp.Admin.Web.Services;
using TravelApp.Application.Dtos.Pois;

namespace TravelApp.Admin.Web.Controllers;

[Authorize]
public class PoisController : Controller
{
    private readonly ITravelAppApiClient _apiClient;

    public PoisController(ITravelAppApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _apiClient.GetPoisAsync("vi", cancellationToken);
        return View(model);
    }

    public IActionResult Create()
    {
        return View(new PoiEditorViewModel { PrimaryLanguage = "vi", SpeechTextLanguageCode = "vi" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PoiEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = ToRequest(model);
        var result = await _apiClient.CreatePoiAsync(request, cancellationToken);
        return RedirectToAction("AttachPoi", "Tours", new { poiId = result.Id });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var poi = await _apiClient.GetPoiAsync(id, "vi", cancellationToken);
        if (poi is null)
        {
            return NotFound();
        }

        return View(ToEditorModel(poi));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PoiEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await _apiClient.UpdatePoiAsync(id, ToRequest(model), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _apiClient.DeletePoiAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Không thể xóa POI này vì nó đang được dùng trong Tour.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static PoiEditorViewModel ToEditorModel(PoiMobileDto poi)
    {
        return new PoiEditorViewModel
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            Description = poi.Description,
            Category = poi.Category,
            Location = poi.Location,
            ImageUrl = poi.ImageUrl,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
            PrimaryLanguage = poi.PrimaryLanguage,
            SpeechText = poi.SpeechText,
            SpeechTextLanguageCode = poi.SpeechTextLanguageCode ?? poi.LanguageCode
        };
    }

    private static UpsertPoiRequestDto ToRequest(PoiEditorViewModel model)
    {
        return new UpsertPoiRequestDto
        {
            Title = model.Title,
            Subtitle = model.Subtitle,
            Description = model.Description,
            Category = model.Category,
            Location = model.Location,
            ImageUrl = model.ImageUrl,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            GeofenceRadiusMeters = model.GeofenceRadiusMeters,
            PrimaryLanguage = model.PrimaryLanguage,
            SpeechText = model.SpeechText,
            SpeechTextLanguageCode = model.SpeechTextLanguageCode,
            Localizations = [],
            AudioAssets = [],
            SpeechTexts = []
        };
    }
}
