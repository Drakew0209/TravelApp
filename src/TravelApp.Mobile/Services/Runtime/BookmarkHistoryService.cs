using System.Text.Json;
using TravelApp.Models;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class BookmarkHistoryService : IBookmarkHistoryService
{
    private const string BookmarksPreferenceKey = "bookmark_history_bookmarks_v1";
    private const string HistoryPreferenceKey = "bookmark_history_history_v1";
    private const int MaxHistoryItems = 100;

    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly IPoiApiClient _poiApiClient;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public event EventHandler? Changed;

    public BookmarkHistoryService(ILocalDatabaseService localDatabaseService, IPoiApiClient poiApiClient)
    {
        _localDatabaseService = localDatabaseService;
        _poiApiClient = poiApiClient;
    }

    public async Task<IReadOnlyList<PoiModel>> GetBookmarksAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var bookmarkStates = ReadBookmarks();
            if (bookmarkStates.Count == 0)
            {
                return [];
            }

            return await ResolvePoisAsync(bookmarkStates.OrderByDescending(x => x.SavedAtUtc).Select(x => x.PoiId), languageCode, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<HistoryPoiItem>> GetHistoryAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var historyStates = ReadHistory();
            if (historyStates.Count == 0)
            {
                return [];
            }

            var bookmarks = ReadBookmarks().Select(x => x.PoiId).ToHashSet();
            var historyByPoiId = historyStates
                .OrderByDescending(x => x.VisitedAtUtc)
                .GroupBy(x => x.PoiId)
                .Select(x => x.First())
                .ToList();

            var pois = await ResolvePoisAsync(historyByPoiId.Select(x => x.PoiId), languageCode, cancellationToken);
            var lookup = pois.ToDictionary(x => x.Id);

            var result = new List<HistoryPoiItem>();
            foreach (var history in historyByPoiId)
            {
                if (!lookup.TryGetValue(history.PoiId, out var poi))
                {
                    continue;
                }

                result.Add(new HistoryPoiItem
                {
                    Poi = poi,
                    VisitedAtUtc = history.VisitedAtUtc,
                    IsBookmarked = bookmarks.Contains(history.PoiId)
                });
            }

            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> IsBookmarkedAsync(int poiId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return ReadBookmarks().Any(x => x.PoiId == poiId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ToggleBookmarkAsync(PoiModel poi, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var bookmarks = ReadBookmarks();
            var existing = bookmarks.FirstOrDefault(x => x.PoiId == poi.Id);
            if (existing is not null)
            {
                bookmarks.Remove(existing);
            }
            else
            {
                bookmarks.Add(new BookmarkState(poi.Id, DateTimeOffset.UtcNow));
            }

            SaveBookmarks(bookmarks);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddHistoryAsync(PoiModel poi, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var history = ReadHistory();
            history.RemoveAll(x => x.PoiId == poi.Id);
            history.Insert(0, new HistoryState(poi.Id, DateTimeOffset.UtcNow));

            if (history.Count > MaxHistoryItems)
            {
                history.RemoveRange(MaxHistoryItems, history.Count - MaxHistoryItems);
            }

            SaveHistory(history);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveHistoryAsync(int poiId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var history = ReadHistory();
            history.RemoveAll(x => x.PoiId == poiId);
            SaveHistory(history);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            SaveHistory([]);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task<IReadOnlyList<PoiModel>> ResolvePoisAsync(IEnumerable<int> poiIds, string? languageCode, CancellationToken cancellationToken)
    {
        var idList = poiIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var byId = new Dictionary<int, PoiModel>();

        var localPois = await _localDatabaseService.GetPoisAsync(languageCode, cancellationToken: cancellationToken);
        foreach (var poi in localPois)
        {
            if (idList.Contains(poi.Id))
            {
                byId[poi.Id] = MapFromLocal(poi);
            }
        }

        var missingIds = idList.Where(x => !byId.ContainsKey(x)).ToList();
        if (missingIds.Count > 0 && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            foreach (var missingId in missingIds)
            {
                var remotePoi = await _poiApiClient.GetByIdAsync(missingId, languageCode, cancellationToken);
                if (remotePoi is null)
                {
                    continue;
                }

                byId[missingId] = MapFromRemote(remotePoi);
            }
        }

        return idList.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
    }

    private static PoiModel MapFromLocal(PoiMobileDto poi)
    {
        return new PoiModel
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Distance = string.Empty,
            Duration = string.Empty,
            Description = poi.Description,
            Provider = null,
            Credit = null
        };
    }

    private static PoiModel MapFromRemote(PoiDto poi)
    {
        return new PoiModel
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Distance = poi.Distance,
            Duration = poi.Duration,
            Description = poi.Description,
            Provider = poi.Provider,
            Credit = poi.Credit
        };
    }

    private static List<BookmarkState> ReadBookmarks()
    {
        var json = Preferences.Default.Get(BookmarksPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<BookmarkState>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<HistoryState> ReadHistory()
    {
        var json = Preferences.Default.Get(HistoryPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<HistoryState>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void SaveBookmarks(List<BookmarkState> bookmarks)
    {
        Preferences.Default.Set(BookmarksPreferenceKey, JsonSerializer.Serialize(bookmarks));
    }

    private static void SaveHistory(List<HistoryState> history)
    {
        Preferences.Default.Set(HistoryPreferenceKey, JsonSerializer.Serialize(history));
    }

    private sealed record BookmarkState(int PoiId, DateTimeOffset SavedAtUtc);

    private sealed record HistoryState(int PoiId, DateTimeOffset VisitedAtUtc);
}
