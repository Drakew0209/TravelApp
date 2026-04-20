using System;

namespace TravelApp.Services.Api;

public sealed class PublicWebOptions
{
    private const string DefaultBaseUrl = "https://tigress-dyslexia-maturity.ngrok-free.dev/";

    public string BaseUrl { get; set; } = ResolveDefaultBaseUrl();

    private static string ResolveDefaultBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("TRAVELAPP_PUBLIC_WEB_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configured) && Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var uri))
        {
            return new UriBuilder(uri)
            {
                Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/",
                Query = string.Empty,
                Fragment = string.Empty
            }.Uri.ToString();
        }

        return DefaultBaseUrl;
    }
}
