using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;
using System.Net.Http.Json;

namespace TravelApp.Services.Api;

public class PoiApiClient : ApiClientBase, IPoiApiClient
{
    public PoiApiClient(IHttpClientFactory httpClientFactory, ApiClientOptions options, ITokenStore tokenStore)
        : base(httpClientFactory, options, tokenStore)
    {
    }

    public async Task<IReadOnlyList<PoiDto>> GetAllAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? "api/pois"
            : $"api/pois?lang={Uri.EscapeDataString(languageCode)}";

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken: cancellationToken);
        var page = await ReadAsAsync<PagedResultDto<PoiMobileDto>>(response, cancellationToken);
        var items = (page?.Items ?? []).Select(MapToPoiDto).ToList();
        return NormalizePois(items);
    }

    public async Task<IReadOnlyList<PoiDto>> GetNearbyAsync(NearbyPoiQueryDto query, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var queryString =
            $"lat={query.Latitude}&lng={query.Longitude}&radiusMeters={query.RadiusMeters}";

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            queryString += $"&lang={Uri.EscapeDataString(languageCode)}";
        }

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"api/pois?{queryString}"), cancellationToken: cancellationToken);
        var page = await ReadAsAsync<PagedResultDto<PoiMobileDto>>(response, cancellationToken);
        var items = (page?.Items ?? []).Select(MapToPoiDto).ToList();
        return NormalizePois(items);
    }

    public async Task<PoiDto?> GetByIdAsync(int id, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        var endpoint = string.IsNullOrWhiteSpace(languageCode)
            ? $"api/pois/{id}"
            : $"api/pois/{id}?lang={Uri.EscapeDataString(languageCode)}";

        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken: cancellationToken);
        var poi = await ReadAsAsync<PoiMobileDto>(response, cancellationToken);
        return NormalizePoi(MapToPoiDto(poi));
    }

    public async Task<PoiDto?> CreateAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/pois")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, authorized: true, cancellationToken);
        var poi = await ReadAsAsync<PoiMobileDto>(response, cancellationToken);
        return NormalizePoi(MapToPoiDto(poi));
    }

    public async Task<bool> UpdateAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Put, $"api/pois/{id}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        }, authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/pois/{id}"), authorized: true, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private PoiDto? NormalizePoi(PoiDto? poi)
    {
        if (poi is null)
        {
            return null;
        }

        poi.ImageUrl = NormalizeResourceUrl(poi.ImageUrl);
        return poi;
    }

    private IReadOnlyList<PoiDto> NormalizePois(IReadOnlyList<PoiDto> pois)
    {
        foreach (var poi in pois)
        {
            poi.ImageUrl = NormalizeResourceUrl(poi.ImageUrl);
        }

        return pois;
    }

    private static PoiDto MapToPoiDto(PoiMobileDto? poi)
    {
        if (poi is null)
        {
            return new PoiDto
            {
                Id = 0,
                Title = string.Empty,
                ImageUrl = string.Empty,
                Location = string.Empty
            };
        }

        return new PoiDto
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            Distance = poi.DistanceMeters.HasValue ? $"{poi.DistanceMeters.Value:F0} m" : string.Empty,
            Duration = string.Empty,
            Description = poi.Description,
            Provider = null,
            Credit = null,
            Category = poi.Category,
            PrimaryLanguage = poi.PrimaryLanguage,
            SpeechText = poi.SpeechText,
            SpeechTextLanguageCode = poi.SpeechTextLanguageCode,
            UpdatedAtUtc = poi.UpdatedAtUtc,
            AudioAssets = poi.AudioAssets.Select(x => new PoiAudioDto(x.LanguageCode, x.AudioUrl, x.Transcript, x.IsGenerated)).ToList(),
            SpeechTexts = poi.SpeechTexts.Select(x => new PoiSpeechTextDto(x.LanguageCode, x.Text)).ToList()
        };
    }
}
