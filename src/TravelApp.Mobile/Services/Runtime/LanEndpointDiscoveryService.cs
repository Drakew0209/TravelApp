using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public sealed class LanEndpointDiscoveryService : ILanEndpointDiscoveryService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(250);
    private const int ApiPort = 5293;
    private const int PublicWebPort = 5175;

    public async Task<LanEndpointDiscoveryResult?> TryDiscoverAsync(CancellationToken cancellationToken = default)
    {
        var candidateIps = GetCandidateIps().Distinct().Take(256).ToList();
        if (candidateIps.Count == 0)
        {
            return null;
        }

        using var httpClient = new HttpClient
        {
            Timeout = RequestTimeout
        };

        var throttler = new SemaphoreSlim(12, 12);
        var tasks = candidateIps.Select(async ip =>
        {
            await throttler.WaitAsync(cancellationToken);
            try
            {
                return await ProbeAsync(httpClient, ip, cancellationToken);
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();

        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            tasks.Remove(finished);

            var result = await finished;
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static async Task<LanEndpointDiscoveryResult?> ProbeAsync(HttpClient httpClient, IPAddress ipAddress, CancellationToken cancellationToken)
    {
        var host = ipAddress.ToString();
        var apiUrl = new UriBuilder(Uri.UriSchemeHttp, host, ApiPort, "/health").Uri;

        try
        {
            using var response = await httpClient.GetAsync(apiUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var publicUrl = new UriBuilder(Uri.UriSchemeHttp, host, PublicWebPort, "/").Uri.ToString();
            return new LanEndpointDiscoveryResult(apiUrl.GetLeftPart(UriPartial.Authority) + "/", publicUrl, host);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<IPAddress> GetCandidateIps()
    {
        foreach (var local in GetLocalIpv4Addresses())
        {
            foreach (var candidate in ExpandSubnet(local))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<IPAddress> GetLocalIpv4Addresses()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            var props = ni.GetIPProperties();
            foreach (var unicast in props.UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address))
                {
                    yield return unicast.Address;
                }
            }
        }
    }

    private static IEnumerable<IPAddress> ExpandSubnet(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            yield break;
        }

        for (var i = 1; i < 255; i++)
        {
            if (bytes[3] == i)
            {
                continue;
            }

            yield return new IPAddress([bytes[0], bytes[1], bytes[2], (byte)i]);
        }
    }
}
