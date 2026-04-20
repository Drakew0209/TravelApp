using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelApp.Admin.Web.Models.Pois;
using TravelApp.Admin.Web.Services;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Utilities;

namespace TravelApp.Admin.Web.Controllers;

[Authorize(Roles = "Owner,Admin,SuperAdmin")]
public class PoisController : Controller
{
    private readonly ITravelAppApiClient _apiClient;

    public PoisController(ITravelAppApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _apiClient.GetPoisAsync(LanguageCodeNormalizer.NormalizeToLocaleCode("en-US"), cancellationToken);
        return View(model);
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public IActionResult Create()
    {
        return View(CreateEmptyModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Create(PoiEditorViewModel model, CancellationToken cancellationToken)
    {
        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            var uploadedUrl = await _apiClient.UploadImageAsync(model.ImageFile, "pois", cancellationToken);
            if (string.IsNullOrWhiteSpace(uploadedUrl))
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Không thể upload ảnh POI.");
            }
            else
            {
                model.ImageUrl = uploadedUrl;
            }
        }

        if (!ModelState.IsValid)
        {
            EnsureMinimumRows(model);
            return View(model);
        }

        var request = ToRequest(model);
        var result = await _apiClient.CreatePoiAsync(request, cancellationToken);
        if (result is null || result.Id <= 0)
        {
            EnsureMinimumRows(model);
            ModelState.AddModelError(string.Empty, "Không thể tạo POI. Vui lòng kiểm tra kết nối API và thử lại.");
            return View(model);
        }

        return RedirectToAction(nameof(Edit), new { id = result.Id });
    }

    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var poi = await _apiClient.GetPoiAsync(id, LanguageCodeNormalizer.NormalizeToLocaleCode("vi"), cancellationToken);
        if (poi is null)
        {
            return NotFound();
        }

        return View(ToEditorModel(poi));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Edit(int id, PoiEditorViewModel model, CancellationToken cancellationToken)
    {
        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            var uploadedUrl = await _apiClient.UploadImageAsync(model.ImageFile, "pois", cancellationToken);
            if (string.IsNullOrWhiteSpace(uploadedUrl))
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Không thể upload ảnh POI.");
            }
            else
            {
                model.ImageUrl = uploadedUrl;
            }
        }

        if (!ModelState.IsValid)
        {
            EnsureMinimumRows(model);
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
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> BackfillSpeechTexts(CancellationToken cancellationToken)
    {
        var updatedCount = await _apiClient.BackfillPoiSpeechTextsAsync(cancellationToken);
        TempData["SuccessMessage"] = $"Đã bổ sung TTS cho {updatedCount} POI.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner,Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _apiClient.DeletePoiAsync(id, cancellationToken);
        if (!deleted)
        {
            TempData["ErrorMessage"] = "Không thể xóa POI này vì nó đang được dùng trong Tour.";
        }

        return RedirectToAction(nameof(Index));
    }

    private PoiEditorViewModel ToEditorModel(PoiMobileDto poi)
    {
        var qrContent = BuildPoiQrContent(poi.Id);
        var model = new PoiEditorViewModel
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
            PrimaryLanguage = NormalizeLanguageCode(poi.PrimaryLanguage),
            SpeechText = poi.SpeechText,
            SpeechTextLanguageCode = NormalizeLanguageCode(poi.SpeechTextLanguageCode ?? poi.LanguageCode),
            Localizations = poi.Localizations.Count > 0
                ? poi.Localizations.Select(x => new PoiLocalizationEditorInput
                {
                    LanguageCode = NormalizeLanguageCode(x.LanguageCode),
                    Title = x.Title,
                    Subtitle = x.Subtitle,
                    Description = x.Description
                }).ToList()
                : [new() { LanguageCode = NormalizeLanguageCode(poi.LanguageCode), Title = poi.Title, Subtitle = poi.Subtitle, Description = poi.Description }],
            AudioAssets = poi.AudioAssets.Count > 0
                ? poi.AudioAssets.Select(x => new PoiAudioEditorInput
                {
                    LanguageCode = NormalizeLanguageCode(x.LanguageCode),
                    AudioUrl = x.AudioUrl,
                    Transcript = x.Transcript
                }).ToList()
                : [new()],
            SpeechTexts = poi.SpeechTexts.Count > 0
                ? poi.SpeechTexts.Select(x => new PoiSpeechTextEditorInput
                {
                    LanguageCode = NormalizeLanguageCode(x.LanguageCode),
                    Text = x.Text
                }).ToList()
                : [new()]
        };

        model.QrContent = qrContent;
        model.QrImageUrl = BuildQrImageUrl(qrContent);

        EnsureMinimumRows(model);
        return model;
    }

    private static PoiEditorViewModel CreateEmptyModel()
    {
        var defaultLanguage = NormalizeLanguageCode("vi");
        var model = new PoiEditorViewModel { PrimaryLanguage = defaultLanguage, SpeechTextLanguageCode = defaultLanguage };
        EnsureMinimumRows(model);
        return model;
    }

    private static void EnsureMinimumRows(PoiEditorViewModel model)
    {
        while (model.Localizations.Count < 1)
        {
            model.Localizations.Add(new PoiLocalizationEditorInput());
        }

        while (model.AudioAssets.Count < 1)
        {
            model.AudioAssets.Add(new PoiAudioEditorInput());
        }

        while (model.SpeechTexts.Count < 1)
        {
            model.SpeechTexts.Add(new PoiSpeechTextEditorInput());
        }
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
            PrimaryLanguage = NormalizeLanguageCode(model.PrimaryLanguage),
            SpeechText = model.SpeechText,
            SpeechTextLanguageCode = NormalizeLanguageCode(model.SpeechTextLanguageCode),
            Localizations = model.Localizations.Select(x => new UpsertPoiLocalizationDto
            {
                LanguageCode = NormalizeLanguageCode(x.LanguageCode),
                Title = x.Title,
                Subtitle = x.Subtitle,
                Description = x.Description
            }).Where(x => !string.IsNullOrWhiteSpace(x.Title)).ToList(),
            AudioAssets = model.AudioAssets.Select(x => new UpsertPoiAudioDto
            {
                LanguageCode = NormalizeLanguageCode(x.LanguageCode),
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                IsGenerated = false
            }).Where(x => !string.IsNullOrWhiteSpace(x.AudioUrl) || !string.IsNullOrWhiteSpace(x.Transcript)).ToList(),
            SpeechTexts = model.SpeechTexts.Select(x => new UpsertPoiSpeechTextDto
            {
                LanguageCode = NormalizeLanguageCode(x.LanguageCode),
                Text = x.Text
            }).Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList()
        };
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        var normalized = LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
        return string.IsNullOrWhiteSpace(normalized) ? "vi-VN" : normalized;
    }

    private string BuildPoiQrContent(int poiId)
    {
        var baseUrl = ResolvePublicWebBaseUrl();
        var builder = new UriBuilder(baseUrl);
        builder.Query = $"poiId={poiId}";
        return builder.Uri.ToString();
    }

    private string ResolvePublicWebBaseUrl()
    {
        if (TryResolveConfiguredPublicWebBaseUrl(out var configuredUrl))
        {
            return configuredUrl;
        }

        if (!string.IsNullOrWhiteSpace(Request?.Host.Host) && !IsLocalHost(Request.Host.Host))
        {
            return new UriBuilder(Uri.UriSchemeHttp, Request.Host.Host, 5175, "/").Uri.ToString();
        }

        var lanIp = GetFirstLanIpv4Address();
        if (!string.IsNullOrWhiteSpace(lanIp))
        {
            return new UriBuilder(Uri.UriSchemeHttp, lanIp, 5175, "/").Uri.ToString();
        }

        return "http://localhost:5175/";
    }

    private static bool TryResolveConfiguredPublicWebBaseUrl(out string normalizedUrl)
    {
        normalizedUrl = string.Empty;
        var configured = Environment.GetEnvironmentVariable("TRAVELAPP_PUBLIC_WEB_BASE_URL");
        if (string.IsNullOrWhiteSpace(configured) || !Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (IsLocalHost(uri.Host))
        {
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };

        normalizedUrl = builder.Uri.ToString();
        return true;
    }

    private static bool IsLocalHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetFirstLanIpv4Address()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            var props = ni.GetIPProperties();
            foreach (var address in props.UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address.Address))
                {
                    continue;
                }

                var bytes = address.Address.GetAddressBytes();
                if (bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254)
                {
                    continue;
                }

                return address.Address.ToString();
            }
        }

        return null;
    }

    private static string BuildQrImageUrl(string qrContent)
    {
        return $"https://quickchart.io/qr?size=260&text={Uri.EscapeDataString(qrContent)}";
    }
}
