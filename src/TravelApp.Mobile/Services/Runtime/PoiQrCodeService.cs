using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using QRCoder;
using TravelApp.Services.Api;
using TravelApp.Services.Abstractions;
using TravelApp.Services;

namespace TravelApp.Services.Runtime;

public sealed class PoiQrCodeService : IPoiQrCodeService
{
    private readonly PublicWebOptions _publicWebOptions;
    private readonly ApiClientOptions _apiClientOptions;

    public PoiQrCodeService(PublicWebOptions publicWebOptions, ApiClientOptions apiClientOptions)
    {
        _publicWebOptions = publicWebOptions;
        _apiClientOptions = apiClientOptions;
    }

    public string BuildPoiShareLink(int poiId, string? languageCode = null)
    {
        if (poiId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poiId));
        }

        var publicWebBaseUrl = ResolvePublicWebBaseUrl();
        if (string.IsNullOrWhiteSpace(publicWebBaseUrl))
        {
            throw new InvalidOperationException("Public web URL must be a valid LAN IP or domain before generating a QR code.");
        }

        var builder = new UriBuilder(publicWebBaseUrl);
        var query = $"poiId={poiId}";
        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        if (!string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            query += $"&lang={Uri.EscapeDataString(normalizedLanguage)}";
        }

        builder.Query = query;
        return builder.Uri.ToString();
    }

    public byte[] GeneratePoiQrCodePng(int poiId, string? languageCode = null)
    {
        return GeneratePoiQrCodePng(BuildPoiShareLink(poiId, languageCode));
    }

    public byte[] GeneratePoiQrCodePng(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("QR content is required.", nameof(content));
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(18);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return LanguageCodeNormalizer.NormalizeToLocaleCode(languageCode);
    }

    private string ResolvePublicWebBaseUrl()
    {
        if (TryNormalizeNonPlaceholderBaseUrl(_publicWebOptions.BaseUrl, out var publicWebBaseUrl))
        {
            return publicWebBaseUrl;
        }

        if (TryBuildPublicWebBaseUrlFromApi(_apiClientOptions.BaseUrl, out var fromApiBaseUrl))
        {
            return fromApiBaseUrl;
        }

        if (TryGetLanIpv4Address(out var lanIp))
        {
            return new UriBuilder(Uri.UriSchemeHttp, lanIp, 5175, "/").Uri.ToString();
        }

        return string.Empty;
    }

    private static bool TryBuildPublicWebBaseUrlFromApi(string? apiBaseUrl, out string publicWebBaseUrl)
    {
        if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) && !IsPlaceholderHost(apiUri.Host))
        {
            publicWebBaseUrl = new UriBuilder(Uri.UriSchemeHttp, apiUri.Host, 5175, "/").Uri.ToString();
            return true;
        }

        publicWebBaseUrl = string.Empty;
        return false;
    }

    private static bool TryNormalizeNonPlaceholderBaseUrl(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || IsPlaceholderHost(uri.Host))
        {
            return false;
        }

        normalized = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/",
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri.ToString();

        return true;
    }

    private static bool IsPlaceholderHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase);
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
}
