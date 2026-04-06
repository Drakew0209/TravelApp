namespace TravelApp.Models.Runtime;

public sealed class AudioDownloadProgressChangedEventArgs : EventArgs
{
    public AudioDownloadProgressChangedEventArgs(
        int poiId,
        double progress,
        bool isCompleted,
        bool isFailed,
        string? message = null,
        int pendingQueueCount = 0,
        TimeSpan? estimatedRemaining = null)
    {
        PoiId = poiId;
        Progress = progress;
        IsCompleted = isCompleted;
        IsFailed = isFailed;
        Message = message;
        PendingQueueCount = pendingQueueCount;
        EstimatedRemaining = estimatedRemaining;
    }

    public int PoiId { get; }
    public double Progress { get; }
    public bool IsCompleted { get; }
    public bool IsFailed { get; }
    public string? Message { get; }
    public int PendingQueueCount { get; }
    public TimeSpan? EstimatedRemaining { get; }
}
