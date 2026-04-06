namespace TravelApp.Models.Runtime;

public sealed class AudioPlaybackStateChangedEventArgs : EventArgs
{
    public AudioPlaybackStateChangedEventArgs(bool isPlaying, int? poiId, string? poiTitle)
    {
        IsPlaying = isPlaying;
        PoiId = poiId;
        PoiTitle = poiTitle;
    }

    public bool IsPlaying { get; }
    public int? PoiId { get; }
    public string? PoiTitle { get; }
}
