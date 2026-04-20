using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class AudioLibraryService : IAudioLibraryService
{
    private const string FallbackAudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3";

    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly IPoiApiClient _poiApiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAudioPlayerService _audioPlayerService;
    private readonly ILogger<AudioLibraryService> _logger;

    private readonly object _sync = new();
    private readonly List<DownloadQueueItem> _queue = [];
    private readonly HashSet<string> _queueKeys = [];
    private readonly HashSet<string> _failedKeys = [];

    private int _isQueueProcessing;
    private double _averageBytesPerSecond;
    private long _averageBytesPerDownload;
    private int _completedDownloads;
    private string _scopeKey = string.Empty;

    public event EventHandler? LibraryChanged;
    public event EventHandler<AudioDownloadProgressChangedEventArgs>? DownloadProgressChanged;

    public AudioLibraryService(
        ILocalDatabaseService localDatabaseService,
        IPoiApiClient poiApiClient,
        IHttpClientFactory httpClientFactory,
        IAudioPlayerService audioPlayerService,
        ILogger<AudioLibraryService> logger)
    {
        _localDatabaseService = localDatabaseService;
        _poiApiClient = poiApiClient;
        _httpClientFactory = httpClientFactory;
        _audioPlayerService = audioPlayerService;
        _logger = logger;

        _scopeKey = UserStorageScope.GetCurrentScopeKey();
        AuthStateService.AuthStateChanged += OnUserContextChanged;
        UserProfileService.ProfileChanged += OnUserContextChanged;
        RestoreQueueState();
        _ = ProcessQueueAsync(CancellationToken.None);
    }

    public async Task<IReadOnlyList<AudioLibraryItem>> GetLibraryItemsAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var pois = await EnsureCatalogueAsync(normalizedLanguage, cancellationToken);

        var items = new List<AudioLibraryItem>(pois.Count);
        HashSet<int> queuedPoiIds;
        lock (_sync)
        {
            queuedPoiIds = _queue.Select(x => x.PoiId).ToHashSet();
        }

        foreach (var poi in pois)
        {
            var path = await _localDatabaseService.GetOfflineAudioPathAsync(poi.Id, normalizedLanguage, cancellationToken);
            var cacheState = await _localDatabaseService.GetAudioDownloadCacheStateAsync(poi.Id, normalizedLanguage, cancellationToken);
            var downloaded = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
            var size = downloaded ? new FileInfo(path!).Length : 0;
            var hasPartial = cacheState is not null && !cacheState.IsCompleted && cacheState.BytesDownloaded > 0;

            items.Add(new AudioLibraryItem
            {
                PoiId = poi.Id,
                Title = poi.Title,
                Subtitle = poi.Subtitle,
                Location = poi.Location,
                ImageUrl = poi.ImageUrl,
                LanguageCode = normalizedLanguage,
                AudioUrl = SelectAudioUrl(poi, normalizedLanguage),
                IsDownloaded = downloaded,
                LocalFilePath = path,
                FileSizeBytes = size,
                IsBusy = queuedPoiIds.Contains(poi.Id) || hasPartial,
                DownloadProgress = hasPartial && cacheState!.BytesDownloaded > 0 ? 0.001 : 0,
                DownloadStatusText = queuedPoiIds.Contains(poi.Id)
                    ? "Đang chờ tải..."
                    : hasPartial ? $"Đã tải tạm {FormatBytes(cacheState!.BytesDownloaded)}" : string.Empty
            });
        }

        return items
            .OrderByDescending(x => x.IsDownloaded)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> DownloadAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var state = await _localDatabaseService.GetAudioDownloadCacheStateAsync(poiId, normalizedLanguage, cancellationToken);
        if (state is not null && state.IsCompleted && !string.IsNullOrWhiteSpace(state.LocalFilePath) && File.Exists(state.LocalFilePath))
        {
            EmitProgress(poiId, 1, isCompleted: true, isFailed: false, "Đã có sẵn trong cache.", GetQueueCount(), TimeSpan.Zero);
            return false;
        }

        var queued = await EnqueueDownloadsAsync([poiId], languageCode, cancellationToken);
        return queued > 0;
    }

    public Task<int> DownloadManyAsync(IEnumerable<int> poiIds, string? languageCode, CancellationToken cancellationToken = default)
    {
        return EnqueueDownloadsAsync(poiIds, languageCode, cancellationToken);
    }

    public Task<int> EnqueueDownloadsAsync(IEnumerable<int> poiIds, string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        var normalizedLanguage = NormalizeLanguage(languageCode);
        var queuedCount = 0;

        lock (_sync)
        {
            foreach (var poiId in poiIds.Distinct())
            {
                var key = BuildStateKey(poiId, normalizedLanguage);
                if (!_queueKeys.Add(key))
                {
                    continue;
                }

                _queue.Add(new DownloadQueueItem(poiId, normalizedLanguage));
                _failedKeys.Remove(key);
                queuedCount++;
            }

            PersistStateLocked();
        }

        if (queuedCount > 0)
        {
            EmitQueueStatus("Đã thêm vào hàng chờ tải.");
            _ = ProcessQueueAsync(CancellationToken.None);
        }

        return Task.FromResult(queuedCount);
    }

    public Task<int> RetryFailedAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        var normalizedLanguage = NormalizeLanguage(languageCode);
        List<DownloadQueueItem> retryTargets;

        lock (_sync)
        {
            retryTargets = _failedKeys
                .Select(ParseStateKey)
                .Where(x => x is not null)
                .Select(x => x!)
                .Where(x => string.Equals(x.LanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in retryTargets)
            {
                _failedKeys.Remove(BuildStateKey(item.PoiId, item.LanguageCode));
            }

            PersistStateLocked();
        }

        if (retryTargets.Count == 0)
        {
            return Task.FromResult(0);
        }

        return EnqueueDownloadsAsync(retryTargets.Select(x => x.PoiId), normalizedLanguage, cancellationToken);
    }

    public async Task<bool> RemoveDownloadAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var existingPath = await _localDatabaseService.GetOfflineAudioPathAsync(poiId, normalizedLanguage, cancellationToken);
        if (string.IsNullOrWhiteSpace(existingPath))
        {
            return false;
        }

        try
        {
            if (File.Exists(existingPath))
            {
                File.Delete(existingPath);
            }
            var tempPath = existingPath + ".part";
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            var pois = await EnsureCatalogueAsync(normalizedLanguage, cancellationToken);
            var poi = pois.FirstOrDefault(x => x.Id == poiId);
            var audioUrl = poi is null ? null : SelectAudioUrl(poi, normalizedLanguage);
            await _localDatabaseService.SaveAudioMetadataAsync(
                poiId,
                normalizedLanguage,
                audioUrl,
                null,
                tempFilePath: null,
                cacheVersionToken: null,
                contentHash: null,
                bytesDownloaded: 0,
                isCompleted: false,
                cancellationToken);

            EmitProgress(poiId, 0, isCompleted: true, isFailed: false, "Đã xóa offline.");
            LibraryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio library remove failed for POI {PoiId}.", poiId);
            return false;
        }
    }

    public async Task<bool> PlayAsync(int poiId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var pois = await EnsureCatalogueAsync(normalizedLanguage, cancellationToken);
        var poi = pois.FirstOrDefault(x => x.Id == poiId);

        if (poi is null)
        {
            return false;
        }

        await _audioPlayerService.PlayAsync(new AudioTriggerRequest(
            ToContractPoi(poi),
            new LocationSample(poi.Latitude, poi.Longitude, DateTimeOffset.UtcNow),
            normalizedLanguage,
            DateTimeOffset.UtcNow), cancellationToken);
        return true;
    }

    public async Task<int> GetDownloadedCountAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        var items = await GetLibraryItemsAsync(languageCode, cancellationToken);
        return items.Count(x => x.IsDownloaded);
    }

    public Task<int> GetFailedCountAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var normalizedLanguage = NormalizeLanguage(languageCode);

        lock (_sync)
        {
            var count = _failedKeys
                .Select(ParseStateKey)
                .Where(x => x is not null && string.Equals(x.LanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
                .Count();

            return Task.FromResult(count);
        }
    }

    public Task<int> GetPendingQueueCountAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var normalizedLanguage = NormalizeLanguage(languageCode);

        lock (_sync)
        {
            var count = _queue.Count(x => string.Equals(x.LanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(count);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _isQueueProcessing, 1) == 1)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DownloadQueueItem? current;
                int pendingCount;

                lock (_sync)
                {
                    current = _queue.FirstOrDefault();
                    pendingCount = _queue.Count;
                }

                if (current is null)
                {
                    break;
                }

                EmitProgress(current.PoiId, 0, isCompleted: false, isFailed: false, "Đang bắt đầu tải...", pendingCount, null);

                var success = await DownloadInternalAsync(current, cancellationToken);

                lock (_sync)
                {
                    var key = BuildStateKey(current.PoiId, current.LanguageCode);
                    _queue.Remove(current);
                    _queueKeys.Remove(key);

                    if (!success)
                    {
                        _failedKeys.Add(key);
                    }

                    PersistStateLocked();
                }

                LibraryChanged?.Invoke(this, EventArgs.Empty);
                EmitQueueStatus(success ? "Đã hoàn tất 1 mục tải." : "Có mục tải thất bại.");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isQueueProcessing, 0);

            lock (_sync)
            {
                if (_queue.Count > 0)
                {
                    _ = ProcessQueueAsync(CancellationToken.None);
                }
            }
        }
    }

    private async Task<bool> DownloadInternalAsync(DownloadQueueItem item, CancellationToken cancellationToken)
    {
        var pois = await EnsureCatalogueAsync(item.LanguageCode, cancellationToken);
        var poi = pois.FirstOrDefault(x => x.Id == item.PoiId);
        if (poi is null)
        {
            EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Không tìm thấy POI.", GetQueueCount(), null);
            return false;
        }

        var audioUrl = SelectAudioUrl(poi, item.LanguageCode);
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "POI chưa có audio.", GetQueueCount(), null);
            return false;
        }

        var localPath = BuildOfflineAudioPath(item.PoiId, item.LanguageCode, audioUrl);
        var tempPath = BuildTempAudioPath(localPath);
        var cachedState = await _localDatabaseService.GetAudioDownloadCacheStateAsync(item.PoiId, item.LanguageCode, cancellationToken);

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var descriptorResponse = await client.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!descriptorResponse.IsSuccessStatusCode)
            {
                EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Không tải được audio.", GetQueueCount(), null);
                return false;
            }

            var remoteVersionToken = BuildVersionToken(descriptorResponse, audioUrl);
            var contentLength = descriptorResponse.Content.Headers.ContentLength;

            if (cachedState is not null
                && !string.IsNullOrWhiteSpace(cachedState.CacheVersionToken)
                && !string.Equals(cachedState.CacheVersionToken, remoteVersionToken, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteFile(localPath);
                TryDeleteFile(tempPath);
                cachedState = null;
            }

            if (cachedState is not null
                && cachedState.IsCompleted
                && File.Exists(localPath)
                && !string.IsNullOrWhiteSpace(cachedState.AudioUrl)
                && string.Equals(cachedState.AudioUrl, audioUrl, StringComparison.OrdinalIgnoreCase)
                && string.Equals(cachedState.CacheVersionToken, remoteVersionToken, StringComparison.OrdinalIgnoreCase))
            {
                EmitProgress(item.PoiId, 1, isCompleted: true, isFailed: false, "Đã có sẵn trong cache.", Math.Max(0, GetQueueCount() - 1), TimeSpan.Zero);
                return true;
            }

            var existingBytes = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0L;
            if (cachedState is not null && cachedState.BytesDownloaded > 0 && existingBytes > 0)
            {
                existingBytes = Math.Min(cachedState.BytesDownloaded, new FileInfo(tempPath).Length);
            }

            var availableBytes = TryGetAvailableStorageBytes(localPath);
            if (contentLength.HasValue && availableBytes.HasValue && availableBytes.Value < contentLength.Value + (1024L * 1024L))
            {
                EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Không đủ dung lượng để tải audio.", GetQueueCount(), null);
                return false;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, audioUrl);
            if (existingBytes > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);
            }

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
            {
                EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Không tải được audio.", GetQueueCount(), null);
                return false;
            }

            if (existingBytes > 0 && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
            {
                existingBytes = 0;
            }

            long? expectedTotalBytes = contentLength;
            if (existingBytes > 0 && response.StatusCode == System.Net.HttpStatusCode.PartialContent)
            {
                var remaining = response.Content.Headers.ContentRange?.Length;
                if (remaining.HasValue)
                {
                    expectedTotalBytes = existingBytes + remaining.Value;
                }
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStream = existingBytes > 0
                ? new FileStream(tempPath, FileMode.Append, FileAccess.Write, FileShare.None)
                : new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[16 * 1024];
            var totalRead = existingBytes;
            var startedAt = DateTimeOffset.UtcNow;
            var lastPersistedBytes = existingBytes;

            while (true)
            {
                var read = await responseStream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;

                if (expectedTotalBytes.HasValue && expectedTotalBytes.Value > 0)
                {
                    var progress = Math.Min(1d, (double)totalRead / expectedTotalBytes.Value);
                    var eta = ComputeEstimatedRemaining(expectedTotalBytes.Value, totalRead, startedAt, item);
                    EmitProgress(item.PoiId, progress, isCompleted: false, isFailed: false, null, GetQueueCount(), eta);
                }

                if (totalRead - lastPersistedBytes >= 256 * 1024)
                {
                    await _localDatabaseService.SaveAudioMetadataAsync(
                        item.PoiId,
                        item.LanguageCode,
                        audioUrl,
                        null,
                        tempFilePath: tempPath,
                        cacheVersionToken: remoteVersionToken,
                        contentHash: null,
                        bytesDownloaded: totalRead,
                        isCompleted: false,
                        cancellationToken);
                    lastPersistedBytes = totalRead;
                }
            }

            await fileStream.FlushAsync(cancellationToken);

            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }

            File.Move(tempPath, localPath);

            var contentHash = await ComputeFileHashAsync(localPath, cancellationToken);
            await _localDatabaseService.SaveAudioMetadataAsync(
                item.PoiId,
                item.LanguageCode,
                audioUrl,
                localPath,
                tempFilePath: null,
                cacheVersionToken: remoteVersionToken,
                contentHash: contentHash,
                bytesDownloaded: new FileInfo(localPath).Length,
                isCompleted: true,
                cancellationToken);
            UpdateDownloadAverages(totalRead, startedAt);

            EmitProgress(item.PoiId, 1, isCompleted: true, isFailed: false, "Đã tải offline.", Math.Max(0, GetQueueCount() - 1), TimeSpan.Zero);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio library download failed for POI {PoiId}.", item.PoiId);

            try
            {
                if (File.Exists(tempPath))
                {
                    // keep partial file for resume; just preserve it
                }
            }
            catch
            {
            }

            var partialBytes = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0L;
            try
            {
                await _localDatabaseService.SaveAudioMetadataAsync(
                    item.PoiId,
                    item.LanguageCode,
                    audioUrl,
                    null,
                    tempFilePath: tempPath,
                    cacheVersionToken: cachedState?.CacheVersionToken,
                    contentHash: cachedState?.ContentHash,
                    bytesDownloaded: partialBytes,
                    isCompleted: false,
                    cancellationToken);
            }
            catch
            {
            }

            EmitProgress(item.PoiId, 0, isCompleted: true, isFailed: true, "Tải thất bại.", Math.Max(0, GetQueueCount() - 1), null);
            return false;
        }
    }

    private TimeSpan? ComputeEstimatedRemaining(long totalBytes, long currentRead, DateTimeOffset startedAt, DownloadQueueItem currentItem)
    {
        var elapsedSeconds = Math.Max(0.1, (DateTimeOffset.UtcNow - startedAt).TotalSeconds);
        var currentSpeed = currentRead / elapsedSeconds;
        if (currentSpeed <= 1)
        {
            currentSpeed = _averageBytesPerSecond > 1 ? _averageBytesPerSecond : 64 * 1024;
        }

        var currentRemainingBytes = Math.Max(0, totalBytes - currentRead);
        var currentRemainingSeconds = currentRemainingBytes / currentSpeed;

        int remainingQueueCount;
        lock (_sync)
        {
            remainingQueueCount = _queue.Count(x => !(x.PoiId == currentItem.PoiId && string.Equals(x.LanguageCode, currentItem.LanguageCode, StringComparison.OrdinalIgnoreCase))) - 1;
            if (remainingQueueCount < 0)
            {
                remainingQueueCount = 0;
            }
        }

        var averageBytes = _averageBytesPerDownload > 0 ? _averageBytesPerDownload : totalBytes;
        var averageSpeed = _averageBytesPerSecond > 1 ? _averageBytesPerSecond : currentSpeed;

        var queuedRemainingSeconds = remainingQueueCount * (averageBytes / averageSpeed);
        var totalSeconds = currentRemainingSeconds + queuedRemainingSeconds;
        return TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
    }

    private void UpdateDownloadAverages(long bytesDownloaded, DateTimeOffset startedAt)
    {
        var seconds = Math.Max(0.1, (DateTimeOffset.UtcNow - startedAt).TotalSeconds);
        var speed = bytesDownloaded / seconds;

        _completedDownloads++;

        if (_completedDownloads == 1)
        {
            _averageBytesPerDownload = bytesDownloaded;
            _averageBytesPerSecond = speed;
            return;
        }

        _averageBytesPerDownload = (long)((_averageBytesPerDownload * (_completedDownloads - 1) + bytesDownloaded) / (double)_completedDownloads);
        _averageBytesPerSecond = (_averageBytesPerSecond * (_completedDownloads - 1) + speed) / _completedDownloads;
    }

    private async Task<IReadOnlyList<PoiMobileDto>> EnsureCatalogueAsync(string languageCode, CancellationToken cancellationToken)
    {
        var local = await _localDatabaseService.GetPoisAsync(languageCode, cancellationToken: cancellationToken);
        if (local.Count > 0)
        {
            return local;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return [];
        }

        try
        {
            var remote = await _poiApiClient.GetAllAsync(languageCode, cancellationToken);
            var mapped = remote.Select(x => new PoiMobileDto
            {
                Id = x.Id,
                Title = x.Title,
                Subtitle = x.Subtitle,
                Description = x.Description ?? string.Empty,
                SpeechText = x.SpeechText,
                LanguageCode = NormalizeLanguage(x.PrimaryLanguage),
                PrimaryLanguage = NormalizeLanguage(x.PrimaryLanguage),
                ImageUrl = x.ImageUrl,
                Location = x.Location,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                GeofenceRadiusMeters = x.GeofenceRadiusMeters ?? 200,
                Category = x.Category ?? string.Empty,
                AudioAssets = x.AudioAssets.Select(a => new PoiAudioMobileDto
                {
                    LanguageCode = NormalizeLanguage(a.LanguageCode),
                    AudioUrl = a.AudioUrl,
                    Transcript = a.Transcript,
                    IsGenerated = a.IsGenerated
                }).ToList()
            }).ToList();

            await _localDatabaseService.SavePoisAsync(mapped, cancellationToken);
            return mapped;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio library could not sync POI catalogue from API.");
            return [];
        }
    }

    private static string? SelectAudioUrl(PoiMobileDto poi, string languageCode)
    {
        static string? FirstUrl(IEnumerable<PoiAudioMobileDto> assets)
            => assets.Select(x => NormalizeAudioUrl(x.AudioUrl)).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var byRequested = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(byRequested))
        {
            return byRequested;
        }

        var byPrimary = FirstUrl(poi.AudioAssets.Where(x => string.Equals(x.LanguageCode, poi.PrimaryLanguage, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(byPrimary))
        {
            return byPrimary;
        }

        return FirstUrl(poi.AudioAssets);
    }

    private static string? NormalizeAudioUrl(string? audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(audioUrl, UriKind.Absolute, out var uri))
        {
            return FallbackAudioUrl;
        }

        if (uri.Host.Contains("blob.core.windows.net", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Contains("travel-app-audios", StringComparison.OrdinalIgnoreCase))
        {
            return FallbackAudioUrl;
        }

        return audioUrl;
    }

    private static PoiDto ToContractPoi(PoiMobileDto poi)
    {
        return new PoiDto
        {
            Id = poi.Id,
            Title = poi.Title,
            Subtitle = poi.Subtitle,
            Description = poi.Description,
            Category = poi.Category,
            ImageUrl = poi.ImageUrl,
            Location = poi.Location,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
            Duration = string.Empty,
            Provider = null,
            Credit = null,
            PrimaryLanguage = poi.PrimaryLanguage,
            SpeechText = poi.SpeechText,
            AudioAssets = poi.AudioAssets.Select(audio => new PoiAudioDto(audio.LanguageCode, audio.AudioUrl, audio.Transcript, audio.IsGenerated)).ToList(),
            Localizations = []
        };
    }

    private static string BuildOfflineAudioPath(int poiId, string languageCode, string? url)
    {
        var cacheDirectory = UserStorageScope.GetScopedDirectory(FileSystem.CacheDirectory, "audio");
        var extension = ".mp3";

        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var ext = Path.GetExtension(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                extension = ext;
            }
        }

        return Path.Combine(cacheDirectory, $"poi-{poiId}-{languageCode}{extension}");
    }

    private static string BuildTempAudioPath(string localPath)
    {
        return localPath + ".part";
    }

    private static string BuildVersionToken(HttpResponseMessage response, string audioUrl)
    {
        var etag = response.Headers.ETag?.Tag ?? string.Empty;
        var lastModified = response.Content.Headers.LastModified?.ToString() ?? string.Empty;
        var length = response.Content.Headers.ContentLength?.ToString() ?? string.Empty;
        return $"{audioUrl}|{etag}|{lastModified}|{length}";
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static async Task<string?> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static long? TryGetAvailableStorageBytes(string localPath)
    {
        try
        {
            var root = Path.GetPathRoot(localPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        var kb = bytes / 1024d;
        if (kb < 1024)
        {
            return $"{kb:0.#} KB";
        }

        var mb = kb / 1024d;
        if (mb < 1024)
        {
            return $"{mb:0.#} MB";
        }

        return $"{mb / 1024d:0.#} GB";
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private void EmitProgress(int poiId, double progress, bool isCompleted, bool isFailed, string? message, int pendingQueueCount = 0, TimeSpan? eta = null)
    {
        DownloadProgressChanged?.Invoke(this, new AudioDownloadProgressChangedEventArgs(poiId, progress, isCompleted, isFailed, message, pendingQueueCount, eta));
    }

    private void EmitQueueStatus(string message)
    {
        DownloadProgressChanged?.Invoke(this, new AudioDownloadProgressChangedEventArgs(0, 0, false, false, message, GetQueueCount(), null));
    }

    private int GetQueueCount()
    {
        lock (_sync)
        {
            return _queue.Count;
        }
    }

    private void RestoreQueueState()
    {
        lock (_sync)
        {
            var queueRaw = Preferences.Default.Get(GetQueueStatePreferenceKey(), string.Empty);
            if (!string.IsNullOrWhiteSpace(queueRaw))
            {
                try
                {
                    var keys = JsonSerializer.Deserialize<List<string>>(queueRaw) ?? [];
                    foreach (var key in keys)
                    {
                        var item = ParseStateKey(key);
                        if (item is null)
                        {
                            continue;
                        }

                        var normalizedKey = BuildStateKey(item.PoiId, item.LanguageCode);
                        if (!_queueKeys.Add(normalizedKey))
                        {
                            continue;
                        }

                        _queue.Add(item);
                    }
                }
                catch
                {
                }
            }

            var failedRaw = Preferences.Default.Get(GetFailedStatePreferenceKey(), string.Empty);
            if (!string.IsNullOrWhiteSpace(failedRaw))
            {
                try
                {
                    var failed = JsonSerializer.Deserialize<List<string>>(failedRaw) ?? [];
                    foreach (var key in failed)
                    {
                        _failedKeys.Add(key);
                    }
                }
                catch
                {
                }
            }
        }
    }

    private void PersistStateLocked()
    {
        var queueKeys = _queue.Select(x => BuildStateKey(x.PoiId, x.LanguageCode)).ToList();
        Preferences.Default.Set(GetQueueStatePreferenceKey(), JsonSerializer.Serialize(queueKeys));
        Preferences.Default.Set(GetFailedStatePreferenceKey(), JsonSerializer.Serialize(_failedKeys.ToList()));
    }

    private static string BuildStateKey(int poiId, string languageCode)
    {
        return $"{poiId}|{NormalizeLanguage(languageCode)}";
    }

    private static DownloadQueueItem? ParseStateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var parts = key.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var poiId))
        {
            return null;
        }

        return new DownloadQueueItem(poiId, NormalizeLanguage(parts[1]));
    }

    private void OnUserContextChanged(object? sender, EventArgs e)
    {
        var nextScopeKey = UserStorageScope.GetCurrentScopeKey();
        if (string.Equals(_scopeKey, nextScopeKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lock (_sync)
        {
            _scopeKey = nextScopeKey;
            _queue.Clear();
            _queueKeys.Clear();
            _failedKeys.Clear();
            _averageBytesPerSecond = 0;
            _averageBytesPerDownload = 0;
            _completedDownloads = 0;
            RestoreQueueState();
            PersistStateLocked();
        }

        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetQueueStatePreferenceKey()
    {
        return $"audio_library_queue_v1::{_scopeKey}";
    }

    private string GetFailedStatePreferenceKey()
    {
        return $"audio_library_failed_v1::{_scopeKey}";
    }

    private sealed record DownloadQueueItem(int PoiId, string LanguageCode);
}
