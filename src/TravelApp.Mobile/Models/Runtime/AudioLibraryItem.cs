namespace TravelApp.Models.Runtime;

public sealed class AudioLibraryItem
{
    public int PoiId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string? AudioUrl { get; set; }
    public bool IsDownloaded { get; set; }
    public string? LocalFilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsBusy { get; set; }
    public double DownloadProgress { get; set; }
    public string DownloadStatusText { get; set; } = string.Empty;
}
