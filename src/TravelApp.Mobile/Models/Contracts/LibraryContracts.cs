namespace TravelApp.Models.Contracts;

public sealed record BookmarkStateDto(int PoiId, DateTimeOffset SavedAtUtc);
public sealed record HistoryStateDto(int PoiId, DateTimeOffset VisitedAtUtc);
