using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Maui.Storage;
using TravelApp.Services.Abstractions;
using TravelApp.Services.Api;

namespace TravelApp.Services.Runtime;

public sealed class EndpointSettingsService : IEndpointSettingsService
{
    private const string ApiBaseUrlKey = "travelapp.endpoint.api_base_url_v1";
    private const string PublicWebBaseUrlKey = "travelapp.endpoint.public_web_base_url_v1";

    private readonly ApiClientOptions _apiClientOptions;
    private readonly PublicWebOptions _publicWebOptions;

    public event EventHandler? SettingsChanged;

    public string ApiBaseUrl => _apiClientOptions.BaseUrl;
    public string PublicWebBaseUrl => _publicWebOptions.BaseUrl;

    public EndpointSettingsService(ApiClientOptions apiClientOptions, PublicWebOptions publicWebOptions)
    {
        _apiClientOptions = apiClientOptions;
        _publicWebOptions = publicWebOptions;

        Load();
    }

    public void Update(string apiBaseUrl, string publicWebBaseUrl)
    {
        var normalizedApi = NormalizeBaseUrl(apiBaseUrl, _apiClientOptions.BaseUrl);
        var normalizedPublic = ResolvePublicWebBaseUrl(publicWebBaseUrl, normalizedApi, _publicWebOptions.BaseUrl);

        _apiClientOptions.BaseUrl = normalizedApi;
        _publicWebOptions.BaseUrl = normalizedPublic;

        Preferences.Default.Set(ApiBaseUrlKey, normalizedApi);
        Preferences.Default.Set(PublicWebBaseUrlKey, normalizedPublic);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetToDefaults()
    {
        var defaultApi = ResolveDefaultApiBaseUrl();
        var defaultPublic = ResolvePublicWebBaseUrl(null, defaultApi, _publicWebOptions.BaseUrl, forceSyncFromApi: true);
        Update(defaultApi, defaultPublic);
    }

    private void Load()
    {
        var defaultApi = ResolveDefaultApiBaseUrl();

        var api = Preferences.Default.Get(ApiBaseUrlKey, defaultApi);
        var publicWeb = Preferences.Default.Get(PublicWebBaseUrlKey, _publicWebOptions.BaseUrl);

        var normalizedApi = NormalizeBaseUrlOrDefault(api, defaultApi);
        var normalizedPublic = ResolvePublicWebBaseUrl(publicWeb, normalizedApi, _publicWebOptions.BaseUrl);

        _apiClientOptions.BaseUrl = normalizedApi;
        _publicWebOptions.BaseUrl = normalizedPublic;

        Preferences.Default.Set(ApiBaseUrlKey, normalizedApi);
        Preferences.Default.Set(PublicWebBaseUrlKey, normalizedPublic);
    }

    private static string ResolveDefaultApiBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("TRAVELAPP_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return NormalizeBaseUrl(configured, "http://localhost:5293/");
        }

        if (TryGetLanIpv4Address(out var lanIp))
        {
            return new UriBuilder(Uri.UriSchemeHttp, lanIp, 5293, "/").Uri.ToString();
        }

        return "http://localhost:5293/";
    }

    private static string BuildDefaultPublicWebBaseUrl(string apiBaseUrl)
    {
        var configured = Environment.GetEnvironmentVariable("TRAVELAPP_PUBLIC_WEB_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return NormalizeBaseUrl(configured, "http://localhost:5175/");
        }

        if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
            && !IsPlaceholderHost(apiUri.Host))
        {
            var builder = new UriBuilder(apiUri)
            {
                Port = 5175,
                Path = "/",
                Query = string.Empty,
                Fragment = string.Empty
            };

            return builder.Uri.ToString();
        }

        if (TryGetLanIpv4Address(out var lanIp))
        {
            return new UriBuilder(Uri.UriSchemeHttp, lanIp, 5175, "/").Uri.ToString();
        }

        return "http://localhost:5175/";
    }

    private static string ResolvePublicWebBaseUrl(string? configuredValue, string apiBaseUrl, string defaultPublicBaseUrl, bool forceSyncFromApi = false)
    {
        var envOverride = Environment.GetEnvironmentVariable("TRAVELAPP_PUBLIC_WEB_BASE_URL");
        if (!string.IsNullOrWhiteSpace(envOverride) && IsExternalPublicHost(envOverride))
        {
            return NormalizeBaseUrl(envOverride, BuildDefaultPublicWebBaseUrl(apiBaseUrl));
        }

        if (!forceSyncFromApi && !string.IsNullOrWhiteSpace(configuredValue))
        {
            var normalizedConfigured = NormalizeBaseUrl(configuredValue, BuildDefaultPublicWebBaseUrl(apiBaseUrl));
            if (IsExternalPublicHost(normalizedConfigured))
            {
                return normalizedConfigured;
            }
        }

        if (IsExternalPublicHost(defaultPublicBaseUrl))
        {
            return NormalizeBaseUrl(defaultPublicBaseUrl, BuildDefaultPublicWebBaseUrl(apiBaseUrl));
        }

        return BuildDefaultPublicWebBaseUrl(apiBaseUrl);
    }

    private static string NormalizeBaseUrlOrDefault(string? value, string fallback)
    {
        var normalized = NormalizeBaseUrl(value, fallback);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) || IsPlaceholderHost(uri.Host))
        {
            return fallback;
        }

        return normalized;
    }

    private static bool IsPlaceholderHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExternalPublicHost(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (IsPlaceholderHost(uri.Host))
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var ip))
        {
            var bytes = ip.GetAddressBytes();
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                if (bytes[0] == 10
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    || (bytes[0] == 169 && bytes[1] == 254))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool TryGetLanIpv4Address(out string ipAddress)
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            var props = ni.GetIPProperties();
            foreach (var address in props.UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address.Address))
                {
                    continue;
                }

                var bytes = address.Address.GetAddressBytes();
                if (bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254)
                {
                    continue;
                }

                ipAddress = address.Address.ToString();
                return true;
            }
        }

        ipAddress = string.Empty;
        return false;
    }

    private static string NormalizeBaseUrl(string? value, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return fallback;
        }

        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.ToString();
    }
}
