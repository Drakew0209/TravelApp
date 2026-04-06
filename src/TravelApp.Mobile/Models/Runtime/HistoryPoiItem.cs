using TravelApp.Models;

namespace TravelApp.Models.Runtime;

public sealed class HistoryPoiItem
{
    public required PoiModel Poi { get; init; }
    public DateTimeOffset VisitedAtUtc { get; init; }
    public bool IsBookmarked { get; init; }
}
