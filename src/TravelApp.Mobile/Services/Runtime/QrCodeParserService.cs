using System.Text.RegularExpressions;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class QrCodeParserService : IQrCodeParserService
{
    private static readonly Regex PoiIdRegex = new(@"(?<!\d)(\d{1,10})(?!\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public int? TryParsePoiId(string? qrContent)
    {
        if (string.IsNullOrWhiteSpace(qrContent))
        {
            return null;
        }

        var text = qrContent.Trim();

        if (int.TryParse(text, out var directId) && directId > 0)
        {
            return directId;
        }

        if (TryParseFromUri(text, out var uriId))
        {
            return uriId;
        }

        if (TryParseFromKnownPrefix(text, out var prefixedId))
        {
            return prefixedId;
        }

        return TryParseFirstPositiveInteger(text, out var fallbackId) ? fallbackId : null;
    }

    private static bool TryParseFromUri(string text, out int poiId)
    {
        poiId = 0;

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) && !Uri.TryCreate(text, UriKind.Relative, out uri))
        {
            return false;
        }

        var query = uri.Query;
        if (TryGetQueryParameter(query, "poiId", out poiId) ||
            TryGetQueryParameter(query, "id", out poiId) ||
            TryGetQueryParameter(query, "tourId", out poiId))
        {
            return true;
        }

        foreach (var segment in uri.Segments.Reverse())
        {
            var value = segment.Trim('/', '?', '#');
            if (int.TryParse(value, out poiId) && poiId > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseFromKnownPrefix(string text, out int poiId)
    {
        poiId = 0;

        var normalized = text.Replace('?', '/').Replace('=', '/');
        var markerIndex = normalized.IndexOf("poi", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var trailing = normalized[(markerIndex + 3)..].Trim(':', '/', '-', ' ');
        if (int.TryParse(trailing, out poiId) && poiId > 0)
        {
            return true;
        }

        var match = PoiIdRegex.Match(trailing);
        return match.Success && int.TryParse(match.Value, out poiId) && poiId > 0;
    }

    private static bool TryParseFirstPositiveInteger(string text, out int poiId)
    {
        poiId = 0;

        var match = PoiIdRegex.Match(text);
        return match.Success && int.TryParse(match.Value, out poiId) && poiId > 0;
    }

    private static bool TryGetQueryParameter(string? query, string key, out int value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var trimmed = query.TrimStart('?');
        var pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var split = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (split.Length != 2)
            {
                continue;
            }

            if (!string.Equals(Uri.UnescapeDataString(split[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(Uri.UnescapeDataString(split[1]), out value) && value > 0)
            {
                return true;
            }
        }

        return false;
    }
}
