namespace TravelApp.Models.Runtime;

public sealed class AudioDownloadCacheState
{
    public int PoiId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public string? LocalFilePath { get; set; }
    public string? TempFilePath { get; set; }
    public string? CacheVersionToken { get; set; }
    public string? ContentHash { get; set; }
    public long BytesDownloaded { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
