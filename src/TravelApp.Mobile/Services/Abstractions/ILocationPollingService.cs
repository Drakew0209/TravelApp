using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface ILocationPollingService
{
    event Action<LocationSample>? OnLocationUpdated;

    LocationSample? CurrentLocation { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
