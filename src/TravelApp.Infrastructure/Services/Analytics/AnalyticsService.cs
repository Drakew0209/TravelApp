using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TravelApp.Application.Abstractions.Analytics;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Dtos.Analytics;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Services.Analytics;

public sealed class AnalyticsService : IAnalyticsTrackingService, IAnalyticsDashboardService
{
    private readonly ITravelAppDbContext _dbContext;

    public AnalyticsService(ITravelAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task TrackAsync(AnalyticsEventRecordDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.SessionId))
        {
            return;
        }

        var entity = new AnalyticsEvent
        {
            OccurredAtUtc = request.OccurredAtUtc == default ? DateTimeOffset.UtcNow : request.OccurredAtUtc,
            EventType = request.EventType.ToString(),
            Source = request.Source.ToString(),
            UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim(),
            GuestId = string.IsNullOrWhiteSpace(request.GuestId) ? null : request.GuestId.Trim(),
            DeviceId = request.DeviceId.Trim(),
            SessionId = request.SessionId.Trim(),
            PoiId = request.PoiId,
            TourId = request.TourId,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? null : request.MetadataJson
        };

        _dbContext.AnalyticsEvents.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalyticsDashboardDto> GetDashboardAsync(AnalyticsDashboardQueryDto request, CancellationToken cancellationToken = default)
    {
        var toUtc = request.ToUtc ?? DateTimeOffset.UtcNow;
        var fromUtc = request.FromUtc ?? request.Granularity switch
        {
            AnalyticsGranularityDto.Day => toUtc.AddDays(-7),
            AnalyticsGranularityDto.Week => toUtc.AddDays(-30),
            AnalyticsGranularityDto.Month => toUtc.AddDays(-180),
            _ => toUtc.AddDays(-30)
        };

        var events = await _dbContext.AnalyticsEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc <= toUtc)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        var totalEvents = events.Count;
        var uniqueUsers = events
            .Select(x => x.UserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var uniqueGuests = events
            .Select(x => x.GuestId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var poiViews = Count(events, AnalyticsEventTypeDto.PoiViewed);
        var tourViews = Count(events, AnalyticsEventTypeDto.TourViewed);
        var poiListens = Count(events, AnalyticsEventTypeDto.PoiListened);
        var tourListens = Count(events, AnalyticsEventTypeDto.TourListened);
        var qrScans = Count(events, AnalyticsEventTypeDto.QrScanned);

        var bucketed = events
            .GroupBy(x => GetBucketStart(x.OccurredAtUtc.UtcDateTime, request.Granularity))
            .OrderBy(x => x.Key)
            .Select(group => BuildSeriesPoint(group.Key, group, request.Granularity))
            .ToList();

        var poiTopIds = events.Where(x => x.PoiId.HasValue).GroupBy(x => x.PoiId!.Value).OrderByDescending(x => x.Count()).Take(10).Select(x => x.Key).ToList();
        var tourTopIds = events.Where(x => x.TourId.HasValue).GroupBy(x => x.TourId!.Value).OrderByDescending(x => x.Count()).Take(10).Select(x => x.Key).ToList();

        var poiNames = await _dbContext.Pois.AsNoTracking().Where(x => poiTopIds.Contains(x.Id)).Select(x => new { x.Id, x.Title }).ToListAsync(cancellationToken);
        var tourNames = await _dbContext.Tours.AsNoTracking().Where(x => tourTopIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);

        var topPois = poiTopIds.Select(id => new AnalyticsTopItemDto
        {
            PoiId = id,
            Name = poiNames.FirstOrDefault(x => x.Id == id)?.Title ?? $"POI #{id}",
            Count = events.Count(x => x.PoiId == id)
        }).OrderByDescending(x => x.Count).ToList();

        var topTours = tourTopIds.Select(id => new AnalyticsTopItemDto
        {
            TourId = id,
            Name = tourNames.FirstOrDefault(x => x.Id == id)?.Name ?? $"Tour #{id}",
            Count = events.Count(x => x.TourId == id)
        }).OrderByDescending(x => x.Count).ToList();

        var recentEvents = events
            .Take(request.RecentLimit <= 0 ? 25 : Math.Min(request.RecentLimit, 100))
            .Select(x => new AnalyticsRecentEventDto
            {
                Id = x.Id,
                OccurredAtUtc = x.OccurredAtUtc,
                EventType = ParseEventType(x.EventType),
                Source = ParseSource(x.Source),
                ActorLabel = BuildActorLabel(x),
                DeviceId = x.DeviceId,
                SessionId = x.SessionId,
                PoiId = x.PoiId,
                TourId = x.TourId,
                MetadataJson = x.MetadataJson
            })
            .ToList();

        return new AnalyticsDashboardDto
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Granularity = request.Granularity,
            TotalEvents = totalEvents,
            UniqueUsers = uniqueUsers,
            UniqueGuests = uniqueGuests,
            PoiViews = poiViews,
            TourViews = tourViews,
            PoiListens = poiListens,
            TourListens = tourListens,
            QrScans = qrScans,
            Series = bucketed,
            TopPois = topPois,
            TopTours = topTours,
            RecentEvents = recentEvents
        };
    }

    private static int Count(IEnumerable<AnalyticsEvent> events, AnalyticsEventTypeDto type)
    {
        var name = type.ToString();
        return events.Count(x => string.Equals(x.EventType, name, StringComparison.OrdinalIgnoreCase));
    }

    private static DateTimeOffset GetBucketStart(DateTime utcDateTime, AnalyticsGranularityDto granularity)
    {
        return granularity switch
        {
            AnalyticsGranularityDto.Day => new DateTimeOffset(utcDateTime.Date, TimeSpan.Zero),
            AnalyticsGranularityDto.Week => GetWeekStart(utcDateTime),
            AnalyticsGranularityDto.Month => new DateTimeOffset(new DateTime(utcDateTime.Year, utcDateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => new DateTimeOffset(utcDateTime.Date, TimeSpan.Zero)
        };
    }

    private static DateTimeOffset GetWeekStart(DateTime utcDateTime)
    {
        var diff = (7 + (utcDateTime.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = utcDateTime.Date.AddDays(-diff);
        return new DateTimeOffset(monday, TimeSpan.Zero);
    }

    private static AnalyticsSeriesPointDto BuildSeriesPoint(DateTimeOffset bucketStart, IEnumerable<AnalyticsEvent> events, AnalyticsGranularityDto granularity)
    {
        return new AnalyticsSeriesPointDto
        {
            BucketStartUtc = bucketStart,
            Label = granularity switch
            {
                AnalyticsGranularityDto.Day => bucketStart.ToString("dd/MM"),
                AnalyticsGranularityDto.Week => $"W{GetIsoWeek(bucketStart.UtcDateTime)}",
                AnalyticsGranularityDto.Month => bucketStart.ToString("MM/yyyy"),
                _ => bucketStart.ToString("dd/MM")
            },
            TotalEvents = events.Count(),
            PoiViews = Count(events, AnalyticsEventTypeDto.PoiViewed),
            TourViews = Count(events, AnalyticsEventTypeDto.TourViewed),
            PoiListens = Count(events, AnalyticsEventTypeDto.PoiListened),
            TourListens = Count(events, AnalyticsEventTypeDto.TourListened),
            QrScans = Count(events, AnalyticsEventTypeDto.QrScanned)
        };
    }

    private static int GetIsoWeek(DateTime date)
    {
        var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day is DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday or DayOfWeek.Thursday or DayOfWeek.Friday)
        {
            date = date.AddDays(3);
        }

        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static string BuildActorLabel(AnalyticsEvent item)
    {
        if (!string.IsNullOrWhiteSpace(item.UserId))
        {
            return $"User: {item.UserId}";
        }

        if (!string.IsNullOrWhiteSpace(item.GuestId))
        {
            return $"Guest: {item.GuestId}";
        }

        return "Anonymous";
    }

    private static AnalyticsEventTypeDto ParseEventType(string value)
    {
        return Enum.TryParse<AnalyticsEventTypeDto>(value, true, out var result) ? result : AnalyticsEventTypeDto.PoiViewed;
    }

    private static AnalyticsSourceDto ParseSource(string value)
    {
        return Enum.TryParse<AnalyticsSourceDto>(value, true, out var result) ? result : AnalyticsSourceDto.App;
    }
}
