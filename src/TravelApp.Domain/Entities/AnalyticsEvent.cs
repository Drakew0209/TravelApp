namespace TravelApp.Domain.Entities;

public enum AnalyticsEventType
{
    PoiViewed = 1,
    TourViewed = 2,
    PoiListened = 3,
    TourListened = 4,
    QrScanned = 5
}

public enum AnalyticsSource
{
    App = 1,
    Web = 2
}

public class AnalyticsEvent
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? GuestId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public int? PoiId { get; set; }
    public int? TourId { get; set; }
    public string? MetadataJson { get; set; }
}
