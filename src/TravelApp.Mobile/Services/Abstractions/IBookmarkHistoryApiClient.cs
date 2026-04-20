using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface IBookmarkHistoryApiClient
{
    Task<IReadOnlyList<BookmarkStateDto>?> GetBookmarksAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleBookmarkAsync(int poiId, CancellationToken cancellationToken = default);
    Task<bool> RemoveBookmarkAsync(int poiId, CancellationToken cancellationToken = default);
    Task<bool> ClearBookmarksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryStateDto>?> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<bool> AddHistoryAsync(int poiId, CancellationToken cancellationToken = default);
    Task<bool> RemoveHistoryAsync(int poiId, CancellationToken cancellationToken = default);
    Task<bool> ClearHistoryAsync(CancellationToken cancellationToken = default);
}
