namespace TravelApp.Models.Runtime;

public enum CacheMode
{
    OfflineFirst,
    OnlineFirst
}

public sealed class CachePolicyOptions
{
    public CacheMode Mode { get; set; } = CacheMode.OfflineFirst;
}
