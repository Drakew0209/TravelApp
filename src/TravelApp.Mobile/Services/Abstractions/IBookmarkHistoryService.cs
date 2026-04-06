using TravelApp.Models;
using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface IBookmarkHistoryService
{
    event EventHandler? Changed;

    Task<IReadOnlyList<PoiModel>> GetBookmarksAsync(string? languageCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistoryPoiItem>> GetHistoryAsync(string? languageCode, CancellationToken cancellationToken = default);
    Task<bool> IsBookmarkedAsync(int poiId, CancellationToken cancellationToken = default);
    Task ToggleBookmarkAsync(PoiModel poi, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(PoiModel poi, CancellationToken cancellationToken = default);
    Task RemoveHistoryAsync(int poiId, CancellationToken cancellationToken = default);
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);
}
