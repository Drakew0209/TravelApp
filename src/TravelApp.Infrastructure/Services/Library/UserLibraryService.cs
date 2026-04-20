using Microsoft.EntityFrameworkCore;
using TravelApp.Application.Abstractions.Library;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Dtos.Library;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Services.Library;

public sealed class UserLibraryService : IUserLibraryService
{
    private readonly ITravelAppDbContext _dbContext;

    public UserLibraryService(ITravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BookmarkStateDto>> GetBookmarksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserBookmarks
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SavedAtUtc)
            .Select(x => new BookmarkStateDto(x.PoiId, x.SavedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task ToggleBookmarkAsync(Guid userId, int poiId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserBookmarks.FirstOrDefaultAsync(x => x.UserId == userId && x.PoiId == poiId, cancellationToken);
        if (existing is not null)
        {
            _dbContext.UserBookmarks.Remove(existing);
        }
        else
        {
            _dbContext.UserBookmarks.Add(new UserBookmark
            {
                UserId = userId,
                PoiId = poiId,
                SavedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveBookmarkAsync(Guid userId, int poiId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserBookmarks.FirstOrDefaultAsync(x => x.UserId == userId && x.PoiId == poiId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _dbContext.UserBookmarks.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearBookmarksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.UserBookmarks.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return;
        }

        _dbContext.UserBookmarks.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistoryStateDto>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserHistoryEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.VisitedAtUtc)
            .Select(x => new HistoryStateDto(x.PoiId, x.VisitedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task AddHistoryAsync(Guid userId, int poiId, DateTimeOffset? visitedAtUtc = null, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserHistoryEntries.FirstOrDefaultAsync(x => x.UserId == userId && x.PoiId == poiId, cancellationToken);
        if (existing is not null)
        {
            existing.VisitedAtUtc = visitedAtUtc ?? DateTimeOffset.UtcNow;
        }
        else
        {
            _dbContext.UserHistoryEntries.Add(new UserHistoryEntry
            {
                UserId = userId,
                PoiId = poiId,
                VisitedAtUtc = visitedAtUtc ?? DateTimeOffset.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveHistoryAsync(Guid userId, int poiId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserHistoryEntries.FirstOrDefaultAsync(x => x.UserId == userId && x.PoiId == poiId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _dbContext.UserHistoryEntries.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.UserHistoryEntries.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return;
        }

        _dbContext.UserHistoryEntries.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
