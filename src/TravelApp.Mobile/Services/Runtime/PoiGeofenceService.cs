using TravelApp.Models.Contracts;
using TravelApp.Models.Runtime;
using TravelApp.Services.Abstractions;

namespace TravelApp.Services.Runtime;

public class PoiGeofenceService : IPoiGeofenceService
{
    private static readonly TimeSpan EnterDebounce = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan EnterCooldown = TimeSpan.FromMinutes(5);
    private const double DefaultRadiusMeters = 100;

    private readonly object _sync = new();
    private IReadOnlyList<PoiMobileDto> _pois = [];
    private readonly Dictionary<int, PoiGeofenceState> _states = [];
    private LocationSample? _latestLocation;

    public event Action<PoiMobileDto>? OnPoiEntered;

    public void SetPois(IEnumerable<PoiMobileDto> pois)
    {
        var snapshot = pois?.ToList() ?? [];

        lock (_sync)
        {
            _pois = snapshot;

            var validPoiIds = snapshot.Select(x => x.Id).ToHashSet();
            var stalePoiIds = _states.Keys.Where(x => !validPoiIds.Contains(x)).ToList();
            foreach (var stalePoiId in stalePoiIds)
            {
                CancelPendingEnter(_states[stalePoiId]);
                _states.Remove(stalePoiId);
            }

            foreach (var poi in snapshot)
            {
                if (!_states.ContainsKey(poi.Id))
                {
                    _states[poi.Id] = new PoiGeofenceState();
                }
            }
        }
    }

    public void UpdateLocation(LocationSample locationSample)
    {
        lock (_sync)
        {
            _latestLocation = locationSample;

            foreach (var poi in _pois)
            {
                var state = GetState(poi.Id);
                var radiusMeters = poi.GeofenceRadiusMeters > 0 ? poi.GeofenceRadiusMeters : DefaultRadiusMeters;
                var distanceMeters = CalculateDistanceMeters(
                    locationSample.Latitude,
                    locationSample.Longitude,
                    poi.Latitude,
                    poi.Longitude);
                var isInside = distanceMeters <= radiusMeters;

                if (isInside)
                {
                    if (state.IsInside || state.PendingEnter is not null)
                    {
                        continue;
                    }

                    if (IsInCooldown(state))
                    {
                        state.IsInside = true;
                        continue;
                    }

                    state.PendingEnter = ScheduleDebouncedEnter(poi, state);
                    continue;
                }

                CancelPendingEnter(state);
                state.IsInside = false;
            }
        }
    }

    private CancellationTokenSource ScheduleDebouncedEnter(PoiMobileDto poi, PoiGeofenceState state)
    {
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(EnterDebounce, cts.Token);
                TryCommitEnter(poi, cts);
            }
            catch (OperationCanceledException)
            {
            }
        }, cts.Token);

        return cts;
    }

    private void TryCommitEnter(PoiMobileDto poi, CancellationTokenSource pendingCts)
    {
        Action<PoiMobileDto>? callback = null;

        lock (_sync)
        {
            var state = GetState(poi.Id);
            if (!ReferenceEquals(state.PendingEnter, pendingCts))
            {
                return;
            }

            state.PendingEnter = null;
            pendingCts.Dispose();

            if (_latestLocation is null)
            {
                return;
            }

            var radiusMeters = poi.GeofenceRadiusMeters > 0 ? poi.GeofenceRadiusMeters : DefaultRadiusMeters;
            var distanceMeters = CalculateDistanceMeters(
                _latestLocation.Latitude,
                _latestLocation.Longitude,
                poi.Latitude,
                poi.Longitude);
            var isStillInside = distanceMeters <= radiusMeters;

            if (!isStillInside)
            {
                return;
            }

            if (IsInCooldown(state))
            {
                state.IsInside = true;
                return;
            }

            state.IsInside = true;
            state.LastEnterAtUtc = DateTimeOffset.UtcNow;
            callback = OnPoiEntered;
        }

        callback?.Invoke(poi);
    }

    private PoiGeofenceState GetState(int poiId)
    {
        if (_states.TryGetValue(poiId, out var state))
        {
            return state;
        }

        state = new PoiGeofenceState();
        _states[poiId] = state;
        return state;
    }

    private static bool IsInCooldown(PoiGeofenceState state)
    {
        if (!state.LastEnterAtUtc.HasValue)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - state.LastEnterAtUtc.Value < EnterCooldown;
    }

    private static void CancelPendingEnter(PoiGeofenceState state)
    {
        if (state.PendingEnter is null)
        {
            return;
        }

        state.PendingEnter.Cancel();
        state.PendingEnter.Dispose();
        state.PendingEnter = null;
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        static double ToRadians(double value) => value * Math.PI / 180;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private sealed class PoiGeofenceState
    {
        public bool IsInside { get; set; }
        public DateTimeOffset? LastEnterAtUtc { get; set; }
        public CancellationTokenSource? PendingEnter { get; set; }
    }
}
