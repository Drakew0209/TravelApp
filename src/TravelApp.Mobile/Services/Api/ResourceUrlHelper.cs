namespace TravelApp.Services.Api;

public static class ResourceUrlHelper
{
    public static string Normalize(string? url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            return url.Trim();
        }

        if (!uri.IsAbsoluteUri)
        {
            return new Uri(new Uri(baseUrl), url).ToString();
        }

        if (IsLocalResourceHost(uri.Host))
        {
            var baseUri = new Uri(baseUrl);
            var builder = new UriBuilder(baseUri)
            {
                Path = uri.AbsolutePath,
                Query = uri.Query.TrimStart('?'),
                Fragment = uri.Fragment
            };

            return builder.Uri.ToString();
        }

        return uri.ToString();
    }

    private static bool IsLocalResourceHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase)
            || host.Equals("10.0.3.2", StringComparison.OrdinalIgnoreCase);
    }
}
