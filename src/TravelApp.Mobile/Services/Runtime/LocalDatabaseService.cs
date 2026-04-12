using System.Text.Json;
using SQLite;
using TravelApp.Models.Contracts;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class LocalDatabaseService : ILocalDatabaseService
{
    private const double EarthRadiusMeters = 6371000;
    private const string DatabaseFileName = "travelapp-local.db3";

    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly string _databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
    private SQLiteAsyncConnection? _database;

    public async Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(
        string? languageCode,
        double? latitude = null,
        double? longitude = null,
        double? radiusMeters = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var db = _database!;
        var pois = await db.Table<LocalPoiEntity>().ToListAsync();
        if (pois.Count == 0)
        {
            return [];
        }

        var localizations = await db.Table<LocalPoiLocalizationEntity>().ToListAsync();
        var audios = await db.Table<LocalPoiAudioMetadataEntity>().ToListAsync();

        var requestedLanguage = NormalizeLanguage(languageCode);
        var mapped = pois.Select(poi =>
        {
            var poiLocalizations = localizations.Where(x => x.PoiId == poi.Id).ToList();
            var selectedLocalization = ResolveLocalization(poiLocalizations, requestedLanguage, poi.PrimaryLanguage);
            var poiAudios = audios.Where(x => x.PoiId == poi.Id).Select(x => new PoiAudioMobileDto
            {
                Id = x.Id,
                LanguageCode = x.LanguageCode,
                AudioUrl = x.AudioUrl,
                Transcript = x.Transcript,
                IsGenerated = x.IsGenerated
            }).ToList();

            var speechTexts = DeserializeSpeechTexts(poi.SpeechTextsJson);
            var preferredSpeechLanguage = NormalizeLanguage(poi.SpeechTextLanguageCode);
            var effectiveSpeech = ResolveSpeechText(speechTexts, string.IsNullOrWhiteSpace(preferredSpeechLanguage) ? requestedLanguage : preferredSpeechLanguage, poi.PrimaryLanguage, poi.SpeechText, poi.Description);

            return new PoiMobileDto
            {
                Id = poi.Id,
                Title = selectedLocalization?.Title ?? poi.Title,
                Subtitle = selectedLocalization?.Subtitle ?? poi.Subtitle,
                Description = selectedLocalization?.Description ?? poi.Description,
                LanguageCode = selectedLocalization?.LanguageCode ?? NormalizeLanguage(poi.PrimaryLanguage),
                PrimaryLanguage = NormalizeLanguage(poi.PrimaryLanguage),
                ImageUrl = poi.ImageUrl,
                Location = poi.Location,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
                Category = poi.Category,
                UpdatedAtUtc = poi.UpdatedAtUtc,
                SpeechText = effectiveSpeech.Text,
                SpeechTextLanguageCode = effectiveSpeech.LanguageCode,
                AudioAssets = poiAudios,
                SpeechTexts = speechTexts
            };
        }).ToList();

        if (latitude.HasValue && longitude.HasValue && radiusMeters.HasValue && radiusMeters > 0)
        {
            var lat = latitude.Value;
            var lng = longitude.Value;
            var radius = radiusMeters.Value;

            mapped = mapped
                .Select(x =>
                {
                    x.DistanceMeters = CalculateDistanceMeters(lat, lng, x.Latitude, x.Longitude);
                    return x;
                })
                .Where(x => x.DistanceMeters.HasValue && x.DistanceMeters.Value <= radius)
                .OrderBy(x => x.DistanceMeters)
                .ToList();
        }
        else
        {
            mapped = mapped.OrderBy(x => x.Id).ToList();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return mapped;
    }

    public async Task SavePoisAsync(IEnumerable<PoiMobileDto> pois, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        var db = _database!;
        var snapshot = pois?.ToList() ?? [];

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            foreach (var poi in snapshot)
            {
                await db.InsertOrReplaceAsync(new LocalPoiEntity
                {
                    Id = poi.Id,
                    Title = poi.Title,
                    Subtitle = poi.Subtitle,
                    Description = poi.Description,
                    SpeechText = poi.SpeechText,
                    SpeechTextsJson = SerializeSpeechTexts((poi.SpeechTexts?.Count ?? 0) > 0 ? poi.SpeechTexts : CreateSpeechTextsFromLegacy(poi)),
                    SpeechTextLanguageCode = NormalizeLanguage(poi.SpeechTextLanguageCode),
                    PrimaryLanguage = NormalizeLanguage(poi.PrimaryLanguage),
                    ImageUrl = poi.ImageUrl,
                    Location = poi.Location,
                    Latitude = poi.Latitude,
                    Longitude = poi.Longitude,
                    GeofenceRadiusMeters = poi.GeofenceRadiusMeters,
                    Category = poi.Category,
                    UpdatedAtUtc = poi.UpdatedAtUtc == default ? DateTimeOffset.UtcNow : poi.UpdatedAtUtc
                });

                await db.ExecuteAsync("DELETE FROM LocalPoiLocalization WHERE PoiId = ?", poi.Id);
                await db.InsertOrReplaceAsync(new LocalPoiLocalizationEntity
                {
                    PoiId = poi.Id,
                    LanguageCode = NormalizeLanguage(poi.LanguageCode),
                    Title = poi.Title,
                    Subtitle = poi.Subtitle,
                    Description = poi.Description,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                });

                await db.ExecuteAsync("DELETE FROM LocalPoiAudioMetadata WHERE PoiId = ?", poi.Id);
                foreach (var audio in poi.AudioAssets)
                {
                    await db.InsertOrReplaceAsync(new LocalPoiAudioMetadataEntity
                    {
                        PoiId = poi.Id,
                        LanguageCode = NormalizeLanguage(audio.LanguageCode),
                        AudioUrl = audio.AudioUrl,
                        Transcript = audio.Transcript,
                        IsGenerated = audio.IsGenerated,
                        LocalFilePath = null,
                        UpdatedAtUtc = DateTimeOffset.UtcNow
                    });
                }

                var speechTexts = poi.SpeechTexts?.Count > 0
                    ? poi.SpeechTexts
                    : CreateSpeechTextsFromLegacy(poi);

                await db.ExecuteAsync("DELETE FROM LocalPoiSpeechText WHERE PoiId = ?", poi.Id);
                foreach (var speechText in speechTexts)
                {
                    await db.InsertOrReplaceAsync(new LocalPoiSpeechTextEntity
                    {
                        PoiId = poi.Id,
                        LanguageCode = NormalizeLanguage(speechText.LanguageCode),
                        Text = speechText.Text ?? string.Empty,
                        UpdatedAtUtc = DateTimeOffset.UtcNow
                    });
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<string> ExportDatabaseAsync(string destinationDirectory, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new ArgumentException("Destination directory is required.", nameof(destinationDirectory));
        }

        await EnsureInitializedAsync();
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await CloseDatabaseAsync();

            Directory.CreateDirectory(destinationDirectory);
            var destinationFileName = string.IsNullOrWhiteSpace(fileName)
                ? $"travelapp-local-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.db3"
                : fileName.Trim();
            var destinationPath = Path.Combine(destinationDirectory, destinationFileName);

            File.Copy(_databasePath, destinationPath, true);
            return destinationPath;
        }
        finally
        {
            await EnsureInitializedAsync();
            _writeGate.Release();
        }
    }

    public async Task ImportDatabaseAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path is required.", nameof(sourceFilePath));
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Database file not found.", sourceFilePath);
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await CloseDatabaseAsync();

            Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
            DeleteSidecarFiles(_databasePath);
            File.Copy(sourceFilePath, _databasePath, true);

            await EnsureInitializedAsync();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<string?> GetOfflineAudioPathAsync(int poiId, string languageCode, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var normalizedLanguage = NormalizeLanguage(languageCode);
        var db = _database!;
        var metadata = await db.Table<LocalPoiAudioMetadataEntity>()
            .Where(x => x.PoiId == poiId)
            .ToListAsync();

        cancellationToken.ThrowIfCancellationRequested();

        var match = metadata.FirstOrDefault(x => string.Equals(x.LanguageCode, normalizedLanguage, StringComparison.OrdinalIgnoreCase)
                                                 && !string.IsNullOrWhiteSpace(x.LocalFilePath)
                                                 && File.Exists(x.LocalFilePath));
        if (match is not null)
        {
            return match.LocalFilePath;
        }

        return metadata.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.LocalFilePath) && File.Exists(x.LocalFilePath))?.LocalFilePath;
    }

    public async Task SaveAudioMetadataAsync(
        int poiId,
        string languageCode,
        string? audioUrl,
        string? localFilePath,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var db = _database!;
        var normalizedLanguage = NormalizeLanguage(languageCode);

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            var current = await db.Table<LocalPoiAudioMetadataEntity>()
                .Where(x => x.PoiId == poiId && x.LanguageCode == normalizedLanguage)
                .FirstOrDefaultAsync();

            var entity = current ?? new LocalPoiAudioMetadataEntity
            {
                PoiId = poiId,
                LanguageCode = normalizedLanguage
            };

            entity.AudioUrl = audioUrl;
            entity.LocalFilePath = localFilePath;
            entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await db.InsertOrReplaceAsync(entity);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_database is not null)
        {
            return;
        }

        await _initGate.WaitAsync();
        try
        {
            if (_database is not null)
            {
                return;
            }

            var connection = new SQLiteAsyncConnection(_databasePath);

            await connection.CreateTableAsync<LocalPoiEntity>();
            await connection.CreateTableAsync<LocalPoiLocalizationEntity>();
            await connection.CreateTableAsync<LocalPoiAudioMetadataEntity>();
            await connection.CreateTableAsync<LocalPoiSpeechTextEntity>();

            await EnsurePoiSpeechTextColumnAsync(connection);
            await EnsurePoiSpeechTextsColumnAsync(connection);
            await EnsurePoiSpeechTextLanguageCodeColumnAsync(connection);

            _database = connection;
        }
        finally
        {
            _initGate.Release();
        }
    }

    private async Task CloseDatabaseAsync()
    {
        if (_database is null)
        {
            return;
        }

        var database = _database;
        _database = null;

        try
        {
            await database.CloseAsync();
        }
        catch
        {
        }
    }

    private void DeleteSidecarFiles(string databasePath)
    {
        var walPath = databasePath + "-wal";
        var shmPath = databasePath + "-shm";

        if (File.Exists(walPath))
        {
            File.Delete(walPath);
        }

        if (File.Exists(shmPath))
        {
            File.Delete(shmPath);
        }
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static LocalPoiLocalizationEntity? ResolveLocalization(
        IReadOnlyList<LocalPoiLocalizationEntity> localizations,
        string requestedLanguage,
        string? primaryLanguage)
    {
        var normalizedPrimaryLanguage = NormalizeLanguage(primaryLanguage);

        return localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase))
               ?? localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, normalizedPrimaryLanguage, StringComparison.OrdinalIgnoreCase))
               ?? localizations.FirstOrDefault(x => string.Equals(x.LanguageCode, "en", StringComparison.OrdinalIgnoreCase));
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        static double ToRadians(double value) => value * Math.PI / 180d;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    [Table("LocalPoi")]
    private sealed class LocalPoiEntity
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? SpeechText { get; set; }
        public string? SpeechTextsJson { get; set; }
        public string? SpeechTextLanguageCode { get; set; }
        public string PrimaryLanguage { get; set; } = "en";
        public string ImageUrl { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double GeofenceRadiusMeters { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }

    [Table("LocalPoiSpeechText")]
    private sealed class LocalPoiSpeechTextEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int PoiId { get; set; }

        [Indexed]
        public string LanguageCode { get; set; } = "en";

        public string Text { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }

    [Table("LocalPoiLocalization")]
    private sealed class LocalPoiLocalizationEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int PoiId { get; set; }

        [Indexed]
        public string LanguageCode { get; set; } = "en";

        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }

    [Table("LocalPoiAudioMetadata")]
    private sealed class LocalPoiAudioMetadataEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int PoiId { get; set; }

        [Indexed]
        public string LanguageCode { get; set; } = "en";

        public string? AudioUrl { get; set; }
        public string? Transcript { get; set; }
        public bool IsGenerated { get; set; }
        public string? LocalFilePath { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }

    private static async Task EnsurePoiSpeechTextColumnAsync(SQLiteAsyncConnection connection)
    {
        var columns = await connection.QueryAsync<TableInfoRow>("PRAGMA table_info(LocalPoi)");
        if (columns.Any(x => string.Equals(x.Name, "SpeechText", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await connection.ExecuteAsync("ALTER TABLE LocalPoi ADD COLUMN SpeechText TEXT NULL");
    }

    private static async Task EnsurePoiSpeechTextsColumnAsync(SQLiteAsyncConnection connection)
    {
        var columns = await connection.QueryAsync<TableInfoRow>("PRAGMA table_info(LocalPoi)");
        if (columns.Any(x => string.Equals(x.Name, "SpeechTextsJson", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await connection.ExecuteAsync("ALTER TABLE LocalPoi ADD COLUMN SpeechTextsJson TEXT NULL");
    }

    private static async Task EnsurePoiSpeechTextLanguageCodeColumnAsync(SQLiteAsyncConnection connection)
    {
        var columns = await connection.QueryAsync<TableInfoRow>("PRAGMA table_info(LocalPoi)");
        if (columns.Any(x => string.Equals(x.Name, "SpeechTextLanguageCode", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await connection.ExecuteAsync("ALTER TABLE LocalPoi ADD COLUMN SpeechTextLanguageCode TEXT NULL");
    }

    private static List<PoiSpeechTextMobileDto> DeserializeSpeechTexts(string? json)
    {
        try
        {
            return string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<PoiSpeechTextMobileDto>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeSpeechTexts(IReadOnlyList<PoiSpeechTextMobileDto> speechTexts)
    {
        return JsonSerializer.Serialize(speechTexts);
    }

    private static IReadOnlyList<PoiSpeechTextMobileDto> CreateSpeechTextsFromLegacy(PoiMobileDto poi)
    {
        var legacyText = poi.SpeechText ?? poi.Description;
        if (string.IsNullOrWhiteSpace(legacyText))
        {
            return [];
        }

        var languageCode = NormalizeLanguage(poi.SpeechTextLanguageCode ?? poi.PrimaryLanguage ?? poi.LanguageCode);
        return [new PoiSpeechTextMobileDto { LanguageCode = languageCode, Text = legacyText }];
    }

    private static (string Text, string LanguageCode) ResolveSpeechText(
        IReadOnlyList<PoiSpeechTextMobileDto> speechTexts,
        string requestedLanguage,
        string? primaryLanguage,
        string? legacySpeechText,
        string? fallbackDescription)
    {
        var normalizedRequested = NormalizeLanguage(requestedLanguage);
        var normalizedPrimary = NormalizeLanguage(primaryLanguage);

        var selected = speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguage(x.LanguageCode), normalizedRequested, StringComparison.OrdinalIgnoreCase))
                       ?? speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguage(x.LanguageCode), "vi", StringComparison.OrdinalIgnoreCase))
                       ?? speechTexts.FirstOrDefault(x => string.Equals(NormalizeLanguage(x.LanguageCode), normalizedPrimary, StringComparison.OrdinalIgnoreCase))
                       ?? speechTexts.FirstOrDefault();

        if (selected is not null && !string.IsNullOrWhiteSpace(selected.Text))
        {
            return (selected.Text, NormalizeLanguage(selected.LanguageCode));
        }

        if (!string.IsNullOrWhiteSpace(legacySpeechText))
        {
            return (legacySpeechText!, normalizedPrimary);
        }

        if (!string.IsNullOrWhiteSpace(fallbackDescription))
        {
            return (fallbackDescription!, normalizedPrimary);
        }

        return (string.Empty, normalizedPrimary);
    }

    private sealed class TableInfoRow
    {
        public string Name { get; set; } = string.Empty;
    }
}
