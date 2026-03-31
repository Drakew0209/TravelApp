using TravelApp.Models.Contracts;

namespace TravelApp.Services.Abstractions;

public interface IAudioService
{
    Task PlayPoiAudioAsync(PoiMobileDto poi, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
