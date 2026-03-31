using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface ILocalDatabaseService
{
    Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(
        string? languageCode,
        double? latitude = null,
        double? longitude = null,
        double? radiusMeters = null,
        CancellationToken cancellationToken = default);

    Task SavePoisAsync(IEnumerable<PoiMobileDto> pois, CancellationToken cancellationToken = default);

    Task<string?> GetOfflineAudioPathAsync(int poiId, string languageCode, CancellationToken cancellationToken = default);

    Task SaveAudioMetadataAsync(
        int poiId,
        string languageCode,
        string? audioUrl,
        string? localFilePath,
        CancellationToken cancellationToken = default);
}
