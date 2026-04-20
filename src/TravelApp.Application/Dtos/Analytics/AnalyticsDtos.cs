namespace TravelApp.Application.Dtos.Analytics;

public enum AnalyticsEventTypeDto
{
    PoiViewed = 1,
    TourViewed = 2,
    PoiListened = 3,
    TourListened = 4,
    QrScanned = 5
}

public enum AnalyticsSourceDto
{
    App = 1,
    Web = 2
}

public enum AnalyticsGranularityDto
{
    Day = 1,
    Week = 2,
    Month = 3
}

public sealed class AnalyticsEventRecordDto
{
    public AnalyticsEventTypeDto EventType { get; set; }
    public AnalyticsSourceDto Source { get; set; }
    public string? UserId { get; set; }
    public string? GuestId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public int? PoiId { get; set; }
    public int? TourId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AnalyticsDashboardQueryDto
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public AnalyticsGranularityDto Granularity { get; set; } = AnalyticsGranularityDto.Day;
    public int RecentLimit { get; set; } = 25;
}

public sealed class AnalyticsDashboardDto
{
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public AnalyticsGranularityDto Granularity { get; set; } = AnalyticsGranularityDto.Day;
    public int TotalEvents { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueGuests { get; set; }
    public int PoiViews { get; set; }
    public int TourViews { get; set; }
    public int PoiListens { get; set; }
    public int TourListens { get; set; }
    public int QrScans { get; set; }
    public IReadOnlyList<AnalyticsSeriesPointDto> Series { get; set; } = [];
    public IReadOnlyList<AnalyticsTopItemDto> TopPois { get; set; } = [];
    public IReadOnlyList<AnalyticsTopItemDto> TopTours { get; set; } = [];
    public IReadOnlyList<AnalyticsRecentEventDto> RecentEvents { get; set; } = [];
}

public sealed class AnalyticsSeriesPointDto
{
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset BucketStartUtc { get; set; }
    public int TotalEvents { get; set; }
    public int PoiViews { get; set; }
    public int TourViews { get; set; }
    public int PoiListens { get; set; }
    public int TourListens { get; set; }
    public int QrScans { get; set; }
}

public sealed class AnalyticsTopItemDto
{
    public int? PoiId { get; set; }
    public int? TourId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class AnalyticsRecentEventDto
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public AnalyticsEventTypeDto EventType { get; set; }
    public AnalyticsSourceDto Source { get; set; }
    public string ActorLabel { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public int? PoiId { get; set; }
    public int? TourId { get; set; }
    public string? MetadataJson { get; set; }
}
