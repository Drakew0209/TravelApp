namespace TravelApp.Application.Dtos.Library;

public sealed record BookmarkStateDto(int PoiId, DateTimeOffset SavedAtUtc);
public sealed record HistoryStateDto(int PoiId, DateTimeOffset VisitedAtUtc);
