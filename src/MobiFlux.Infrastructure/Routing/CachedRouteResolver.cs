using System.Collections.Concurrent;
using MobiFlux.Application.Proxies;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure.Routing;

public sealed class CachedRouteResolver(TimeProvider timeProvider, int maxStickyEntries) : IRouteResolver
{
    private readonly object _sync = new();
    private IReadOnlyDictionary<Guid, DeviceNode> _devices = new Dictionary<Guid, DeviceNode>();
    private IReadOnlyDictionary<Guid, ProxyPool> _pools = new Dictionary<Guid, ProxyPool>();
    private readonly ConcurrentDictionary<string, (Guid DeviceId, DateTimeOffset Expires)> _sticky = new();
    private readonly ConcurrentDictionary<Guid, int> _activeConnections = new();
    private int _roundRobin;
    private int _weightedRoundRobin;
    public void Refresh(IEnumerable<DeviceNode> devices, IEnumerable<ProxyPool> pools)
    { lock (_sync) { _devices = devices.ToDictionary(x => x.Id); _pools = pools.ToDictionary(x => x.Id); } }
    public Task<RouteDecision?> ResolveAsync(ProxyEndpoint endpoint, string stickyKey, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, DeviceNode> devices; IReadOnlyDictionary<Guid, ProxyPool> pools;
        lock (_sync) { devices = _devices; pools = _pools; }
        if (endpoint.DeviceId is { } direct && devices.TryGetValue(direct, out var device) && device.ActualState == DeviceActualState.ProxyReady && device.Enabled) return Task.FromResult<RouteDecision?>(new(direct, "direct"));
        if (endpoint.PoolId is not { } poolId || !pools.TryGetValue(poolId, out var pool) || !pool.Enabled) return Task.FromResult<RouteDecision?>(null);
        var candidates = pool.Members.Where(x => x.Enabled && devices.TryGetValue(x.DeviceId, out var device) && device.Enabled && device.ActualState == DeviceActualState.ProxyReady).ToArray();
        if (candidates.Length == 0) return Task.FromResult<RouteDecision?>(null);
        var priority = candidates.Min(candidate => candidate.Priority);
        candidates = candidates.Where(candidate => candidate.Priority == priority).OrderBy(candidate => candidate.DeviceId).ToArray();
        var cacheKey = $"{endpoint.Id:N}:{stickyKey}"; var now = timeProvider.GetUtcNow();
        if (pool.StickyTtlSeconds > 0 && _sticky.TryGetValue(cacheKey, out var stored) && stored.Expires > now && candidates.Any(x => x.DeviceId == stored.DeviceId)) return Task.FromResult<RouteDecision?>(new(stored.DeviceId, "sticky"));
        var chosen = pool.RoutingStrategy switch
        {
            RoutingStrategy.RoundRobin => candidates[PositiveModulo(Interlocked.Increment(ref _roundRobin), candidates.Length)].DeviceId,
            RoutingStrategy.LeastConnections => candidates.OrderBy(candidate => _activeConnections.GetValueOrDefault(candidate.DeviceId)).ThenBy(candidate => candidate.DeviceId).First().DeviceId,
            RoutingStrategy.WeightedRoundRobin => SelectWeighted(candidates).DeviceId,
            RoutingStrategy.Random => candidates[Random.Shared.Next(candidates.Length)].DeviceId,
            _ => throw new ArgumentOutOfRangeException(nameof(pool.RoutingStrategy))
        };
        if (pool.StickyTtlSeconds > 0) AddSticky(cacheKey, chosen, now.AddSeconds(pool.StickyTtlSeconds));
        return Task.FromResult<RouteDecision?>(new(chosen, "pool"));
    }

    public void ConnectionOpened(Guid deviceId) => _activeConnections.AddOrUpdate(deviceId, 1, static (_, current) => current + 1);

    public void ConnectionClosed(Guid deviceId) => _activeConnections.AddOrUpdate(deviceId, 0, static (_, current) => Math.Max(0, current - 1));

    private ProxyPoolMember SelectWeighted(IReadOnlyList<ProxyPoolMember> candidates)
    {
        var totalWeight = candidates.Sum(candidate => candidate.Weight);
        var position = PositiveModulo(Interlocked.Increment(ref _weightedRoundRobin), totalWeight);
        foreach (var candidate in candidates)
        {
            if (position < candidate.Weight) return candidate;
            position -= candidate.Weight;
        }
        throw new InvalidOperationException("Weighted routing requires at least one candidate.");
    }

    private void AddSticky(string cacheKey, Guid deviceId, DateTimeOffset expires)
    {
        if (_sticky.Count >= maxStickyEntries)
        {
            foreach (var entry in _sticky.Where(entry => entry.Value.Expires <= timeProvider.GetUtcNow()).Take(_sticky.Count - maxStickyEntries + 1))
                _sticky.TryRemove(entry.Key, out _);
            if (_sticky.Count >= maxStickyEntries) return;
        }
        _sticky[cacheKey] = (deviceId, expires);
    }

    private static int PositiveModulo(int value, int divisor) => (value & int.MaxValue) % divisor;
}
