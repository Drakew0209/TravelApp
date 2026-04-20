using TravelApp.Models;
using TravelApp.Resources.Strings;

namespace TravelApp.Models.Runtime;

public sealed class HistoryPoiItem
{
    public required PoiModel Poi { get; init; }
    public DateTimeOffset VisitedAtUtc { get; init; }
    public bool IsBookmarked { get; init; }

    public string VisitedText => $"{AppStrings.VisitedPrefix} {VisitedAtUtc:dd/MM HH:mm}";
}
