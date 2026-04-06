using TravelApp.Models.Runtime;

namespace TravelApp.Services.Abstractions;

public interface ITourMapRouteService
{
    event EventHandler<TourMapRouteUpdatedEventArgs>? RouteUpdated;

    TourMapRouteSnapshot? CurrentSnapshot { get; }

    Task StartAsync(string? languageCode, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
