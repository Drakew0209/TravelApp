using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace TravelApp.Services.Api;

public class PoiApiService : ApiClientBase, IPoiApiService
{
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly CachePolicyOptions _cachePolicyOptions;
    private readonly ILogService _logService;
    private readonly ILogger<PoiApiService> _logger;

    public PoiApiService(
        IHttpClientFactory httpClientFactory,
        ApiClientOptions options,
        ITokenStore tokenStore,
        ILocalDatabaseService localDatabaseService,
        CachePolicyOptions cachePolicyOptions,
        ILogService logService,
        ILogger<PoiApiService> logger)
        : base(httpClientFactory, options, tokenStore)
    {
        _localDatabaseService = localDatabaseService;
        _cachePolicyOptions = cachePolicyOptions;
        _logService = logService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        string? languageCode,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = Math.Max(1, pageNumber);
        var effectivePageSize = Math.Clamp(pageSize, 1, 100);

        var cached = await _localDatabaseService.GetPoisAsync(
            languageCode,
            latitude,
            longitude,
            radiusMeters,
            cancellationToken);

        var isOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        if (!isOnline)
        {
            _logger.LogInformation("POI source=local-cache (offline), count={Count}", cached.Count);
            _logService.Log("POI", $"source=local-cache reason=offline count={cached.Count}");
            return ApplyPaging(cached, effectivePageNumber, effectivePageSize);
        }

        if (_cachePolicyOptions.Mode == CacheMode.OfflineFirst && cached.Count > 0)
        {
            _logger.LogInformation("POI source=local-cache (offline-first), count={Count}", cached.Count);
            _logService.Log("POI", $"source=local-cache policy=offline-first count={cached.Count}");

            _ = Task.Run(() => RefreshCacheFromApiAsync(
                    latitude,
                    longitude,
                    radiusMeters,
                    languageCode,
                    effectivePageNumber,
                    effectivePageSize,
                    CancellationToken.None));

            return ApplyPaging(cached, effectivePageNumber, effectivePageSize);
        }

        var onlinePois = await FetchOnlinePoisAsync(
            latitude,
            longitude,
            radiusMeters,
            languageCode,
            effectivePageNumber,
            effectivePageSize,
            cancellationToken);

        if (onlinePois.Count > 0)
        {
            _logger.LogInformation("POI source=api, count={Count}", onlinePois.Count);
            _logService.Log("POI", $"source=api count={onlinePois.Count}");
            return onlinePois;
        }

        _logger.LogInformation("POI source=local-cache (api-empty), count={Count}", cached.Count);
        _logService.Log("POI", $"source=local-cache reason=api-empty count={cached.Count}");
        return ApplyPaging(cached, effectivePageNumber, effectivePageSize);
    }

    private async Task<IReadOnlyList<PoiMobileDto>> FetchOnlinePoisAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        string? languageCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var endpoint = BuildEndpoint(latitude, longitude, radiusMeters, languageCode, pageNumber, pageSize);
        var response = await client.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<PagedResultDto<PoiMobileDto>>(JsonOptions, cancellationToken);
        var items = payload?.Items ?? [];

        if (items.Count > 0)
        {
            await _localDatabaseService.SavePoisAsync(items, cancellationToken);
        }

        return items;
    }

    private async Task RefreshCacheFromApiAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        string? languageCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await FetchOnlinePoisAsync(latitude, longitude, radiusMeters, languageCode, pageNumber, pageSize, cancellationToken);
            _logger.LogInformation("POI cache refresh from API complete, count={Count}", updated.Count);
            _logService.Log("POI", $"cache-refresh source=api count={updated.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "POI cache refresh from API failed.");
        }
    }

    private static IReadOnlyList<PoiMobileDto> ApplyPaging(IReadOnlyList<PoiMobileDto> items, int pageNumber, int pageSize)
    {
        return items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private static string BuildEndpoint(double latitude, double longitude, double radiusMeters, string? languageCode, int pageNumber, int pageSize)
    {
        var sb = new StringBuilder("api/pois?");
        sb.Append("lat=").Append(latitude.ToString("G17", CultureInfo.InvariantCulture));
        sb.Append("&lng=").Append(longitude.ToString("G17", CultureInfo.InvariantCulture));
        sb.Append("&radius=").Append(radiusMeters.ToString("G17", CultureInfo.InvariantCulture));
        sb.Append("&pageNumber=").Append(Math.Max(1, pageNumber));
        sb.Append("&pageSize=").Append(Math.Clamp(pageSize, 1, 100));

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            sb.Append("&lang=").Append(Uri.EscapeDataString(languageCode));
        }

        return sb.ToString();
    }
}
