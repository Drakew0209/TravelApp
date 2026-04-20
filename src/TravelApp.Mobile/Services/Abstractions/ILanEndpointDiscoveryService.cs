namespace TravelApp.Services.Abstractions;

public interface ILanEndpointDiscoveryService
{
    Task<LanEndpointDiscoveryResult?> TryDiscoverAsync(CancellationToken cancellationToken = default);
}

public sealed record LanEndpointDiscoveryResult(string ApiBaseUrl, string PublicWebBaseUrl, string HostIpAddress);
