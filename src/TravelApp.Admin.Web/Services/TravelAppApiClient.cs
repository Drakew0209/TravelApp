using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using TravelApp.Application.Dtos.Analytics;
using TravelApp.Application.Dtos.Pois;
using TravelApp.Application.Dtos.Users;
using TravelApp.Application.Dtos.Tours;

namespace TravelApp.Admin.Web.Services;

public sealed class TravelAppApiClient : ITravelAppApiClient
{
    private readonly HttpClient _httpClient;

    public TravelAppApiClient(HttpClient httpClient, IOptions<TravelAppApiOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
    }

    public async Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(string? languageCode = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = new List<PoiMobileDto>();
            var pageNumber = 1;
            const int pageSize = 100;

            while (true)
            {
                var endpoint = string.IsNullOrWhiteSpace(languageCode)
                    ? $"api/pois?pageNumber={pageNumber}&pageSize={pageSize}"
                    : $"api/pois?lang={Uri.EscapeDataString(languageCode)}&pageNumber={pageNumber}&pageSize={pageSize}";

                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<PagedResultDto<PoiMobileDto>>(cancellationToken: cancellationToken);
                var pageItems = payload?.Items?.ToList() ?? [];
                if (pageItems.Count == 0)
                {
                    break;
                }

                items.AddRange(pageItems);
                if (payload is null || items.Count >= payload.TotalCount)
                {
                    break;
                }

                pageNumber++;
            }

            return items;
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

    public async Task<PoiMobileDto> CreatePoiAsync(UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/pois", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<PoiMobileDto>(cancellationToken: cancellationToken))!;
        }
        catch (HttpRequestException)
        {
            return null!;
        }
    }

    public async Task<bool> UpdatePoiAsync(int id, UpsertPoiRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/pois/{id}", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> DeletePoiAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/pois/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<int> BackfillPoiSpeechTextsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync("api/admin/pois/backfill-speech-texts", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<BackfillSpeechTextsResponseDto>(cancellationToken: cancellationToken);
            return payload?.UpdatedCount ?? 0;
        }
        catch (HttpRequestException)
        {
            return 0;
        }
    }

    public async Task<string?> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                return null;
            }

            await using var stream = file.OpenReadStream();
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var endpoint = string.IsNullOrWhiteSpace(folder)
                ? "api/media/image"
                : $"api/media/image?folder={Uri.EscapeDataString(folder)}";

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<UploadImageResponseDto>(cancellationToken: cancellationToken);
            return payload?.Url;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private sealed class BackfillSpeechTextsResponseDto
    {
        public int UpdatedCount { get; set; }
    }

    public async Task<IReadOnlyList<UserAdminDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserAdminDto>>("api/admin/users", cancellationToken) ?? [];
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

    public async Task<UserAdminDto?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserAdminDto>($"api/admin/users/{id}", cancellationToken);
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

    public async Task<IReadOnlyList<RoleAdminDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RoleAdminDto>>("api/admin/users/roles", cancellationToken) ?? [];
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

    public async Task<UserAdminDto?> CreateUserAsync(UpsertUserRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/users", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return (await response.Content.ReadFromJsonAsync<UserAdminDto>(cancellationToken: cancellationToken))!;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(Guid id, UpsertUserRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{id}", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/users/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<TourAdminDto>> GetToursAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TourAdminDto>>("api/admin/tours", cancellationToken) ?? [];
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

    public async Task<TourAdminDto?> GetTourAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TourAdminDto>($"api/admin/tours/{id}", cancellationToken);
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

    public async Task<TourAdminDto> CreateTourAsync(UpsertTourRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/tours", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<TourAdminDto>(cancellationToken: cancellationToken))!;
        }
        catch (HttpRequestException)
        {
            return null!;
        }
    }

    public async Task<bool> UpdateTourAsync(int id, UpsertTourRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/tours/{id}", request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteTourAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/admin/tours/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<AnalyticsDashboardDto> GetAnalyticsDashboardAsync(AnalyticsDashboardQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new List<string>();

            if (query.FromUtc.HasValue)
            {
                parameters.Add($"fromUtc={Uri.EscapeDataString(query.FromUtc.Value.ToString("O"))}");
            }

            if (query.ToUtc.HasValue)
            {
                parameters.Add($"toUtc={Uri.EscapeDataString(query.ToUtc.Value.ToString("O"))}");
            }

            parameters.Add($"granularity={Uri.EscapeDataString(query.Granularity.ToString())}");
            parameters.Add($"recentLimit={Math.Clamp(query.RecentLimit, 1, 100)}");

            var endpoint = $"api/analytics/dashboard?{string.Join('&', parameters)}";
            return await _httpClient.GetFromJsonAsync<AnalyticsDashboardDto>(endpoint, cancellationToken) ?? new AnalyticsDashboardDto();
        }
        catch (OperationCanceledException)
        {
            return new AnalyticsDashboardDto();
        }
        catch (HttpRequestException)
        {
            return new AnalyticsDashboardDto();
        }
    }

    private sealed record UploadImageResponseDto(string Url);

}
