using TravelApp.Application.Dtos.Library;

namespace TravelApp.Application.Abstractions.Library;

public interface IUserLibraryService
{
    Task<IReadOnlyList<BookmarkStateDto>> GetBookmarksAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ToggleBookmarkAsync(Guid userId, int poiId, CancellationToken cancellationToken = default);
    Task RemoveBookmarkAsync(Guid userId, int poiId, CancellationToken cancellationToken = default);
    Task ClearBookmarksAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryStateDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(Guid userId, int poiId, DateTimeOffset? visitedAtUtc = null, CancellationToken cancellationToken = default);
    Task RemoveHistoryAsync(Guid userId, int poiId, CancellationToken cancellationToken = default);
    Task ClearHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
}
