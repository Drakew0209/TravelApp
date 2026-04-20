using System.Net.Http.Json;
using TravelApp.Application.Dtos.Analytics;
using TravelApp.Models.Contracts;
using TravelApp.Services.Api;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class AnalyticsTrackingService : IAnalyticsTrackingService
{
    private const string DeviceIdKey = "travelapp_analytics_device_id_v1";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiClientOptions _apiOptions;
    private readonly string _deviceId;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    public AnalyticsTrackingService(IHttpClientFactory httpClientFactory, ApiClientOptions apiOptions)
    {
        _httpClientFactory = httpClientFactory;
        _apiOptions = apiOptions;
        _deviceId = ResolveDeviceId();
    }

    public Task TrackPoiViewedAsync(int poiId, string? languageCode = null, CancellationToken cancellationToken = default)
        => TrackAsync(AnalyticsEventTypeDto.PoiViewed, poiId: poiId, languageCode: languageCode, cancellationToken: cancellationToken);

    public Task TrackTourViewedAsync(int tourId, int? poiId = null, string? languageCode = null, CancellationToken cancellationToken = default)
        => TrackAsync(AnalyticsEventTypeDto.TourViewed, poiId: poiId, tourId: tourId, languageCode: languageCode, cancellationToken: cancellationToken);

    public Task TrackPoiListenedAsync(PoiMobileDto poi, string? languageCode = null, CancellationToken cancellationToken = default)
        => TrackAsync(AnalyticsEventTypeDto.PoiListened, poiId: poi.Id, languageCode: languageCode ?? poi.SpeechTextLanguageCode ?? poi.PrimaryLanguage, metadataJson: $"{{\"title\":\"{EscapeJson(poi.Title)}\"}}", cancellationToken: cancellationToken);

    public Task TrackTourListenedAsync(int tourId, int? poiId = null, string? languageCode = null, CancellationToken cancellationToken = default)
        => TrackAsync(AnalyticsEventTypeDto.TourListened, poiId: poiId, tourId: tourId, languageCode: languageCode, cancellationToken: cancellationToken);

    public Task TrackQrScannedAsync(int poiId, string? payload = null, string? languageCode = null, CancellationToken cancellationToken = default)
        => TrackAsync(AnalyticsEventTypeDto.QrScanned, poiId: poiId, languageCode: languageCode, metadataJson: string.IsNullOrWhiteSpace(payload) ? null : $"{{\"payload\":\"{EscapeJson(payload)}\"}}", cancellationToken: cancellationToken);

    private async Task TrackAsync(
        AnalyticsEventTypeDto eventType,
        int? poiId = null,
        int? tourId = null,
        string? languageCode = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiOptions.BaseUrl);

            var request = new AnalyticsEventRecordDto
            {
                EventType = eventType,
                Source = AnalyticsSourceDto.App,
                UserId = AuthStateService.IsLoggedIn ? (string.IsNullOrWhiteSpace(UserProfileService.Email) ? null : UserProfileService.Email) : null,
                GuestId = AuthStateService.IsLoggedIn ? null : _deviceId,
                DeviceId = _deviceId,
                SessionId = _sessionId,
                PoiId = poiId,
                TourId = tourId,
                MetadataJson = metadataJson ?? BuildMetadata(languageCode)
            };

            var response = await client.PostAsJsonAsync("api/analytics/events", request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
        }
    }

    private static string? BuildMetadata(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        return $"{{\"lang\":\"{EscapeJson(languageCode)}\"}}";
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string ResolveDeviceId()
    {
        var existing = Preferences.Default.Get(DeviceIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var deviceId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdKey, deviceId);
        return deviceId;
    }
}
