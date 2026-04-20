using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TravelApp.Application.Dtos.Analytics;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Tours;

namespace TravelApp.Public.Web.Services;

public sealed class TravelAppPublicApiClient : ITravelAppPublicApiClient
{
    private readonly HttpClient _httpClient;

    public TravelAppPublicApiClient(HttpClient httpClient, IOptions<TravelAppApiOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
    }

    public async Task<PoiMobileDto?> GetPoiAsync(int id, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = string.IsNullOrWhiteSpace(languageCode)
                ? $"api/pois/{id}"
                : $"api/pois/{id}?lang={Uri.EscapeDataString(languageCode)}";

            return await _httpClient.GetFromJsonAsync<PoiMobileDto>(endpoint, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TourRouteDto>> GetPublishedToursAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = string.IsNullOrWhiteSpace(languageCode)
                ? "api/tours"
                : $"api/tours?lang={Uri.EscapeDataString(languageCode)}";

            return await _httpClient.GetFromJsonAsync<List<TourRouteDto>>(endpoint, cancellationToken) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<bool> TrackEventAsync(AnalyticsEventRecordDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/analytics/events", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
