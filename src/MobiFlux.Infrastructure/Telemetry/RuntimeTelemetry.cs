using System.Collections.Concurrent;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure.Telemetry;

public sealed class RuntimeTelemetry(TimeProvider clock, int historySamples, Routing.CachedRouteResolver? routes = null, Func<int>? runningListeners = null) : IRuntimeTelemetry
{
    private readonly ConcurrentDictionary<Guid, byte> _active = new();
    private readonly object _sync = new();
    private long _bytesUp, _bytesDown, _opened, _completed, _failed;
    private long _lastStamp = clock.GetTimestamp(), _previousUp, _previousDown;
    public void Open(ProxySessionLease session) { if (_active.TryAdd(session.Id, 0)) Interlocked.Increment(ref _opened); }
    public void Record(long up, long down) { Interlocked.Add(ref _bytesUp, up); Interlocked.Add(ref _bytesDown, down); }
    public void Close(ProxySessionLease session, string reason)
    {
        if (!_active.TryRemove(session.Id, out _)) return;
        if (reason == "completed") Interlocked.Increment(ref _completed); else Interlocked.Increment(ref _failed);
    }
    public RuntimeSnapshotDto Sample()
    {
        lock (_sync)
        {
            var now = clock.GetUtcNow(); var stamp = clock.GetTimestamp();
            var elapsed = clock.GetElapsedTime(_lastStamp, stamp).TotalSeconds;
            var up = Interlocked.Read(ref _bytesUp); var down = Interlocked.Read(ref _bytesDown);
            var sample = new TrafficSampleDto(now, elapsed > 0 ? (up - _previousUp) * 8d / elapsed / 1_000_000 : 0, elapsed > 0 ? (down - _previousDown) * 8d / elapsed / 1_000_000 : 0);
            _lastStamp = stamp; _previousUp = up; _previousDown = down;
            return new(now, _active.Count, up, down, Interlocked.Read(ref _opened), Interlocked.Read(ref _completed), Interlocked.Read(ref _failed), sample, historySamples, routes?.Readiness.Ready ?? 0, runningListeners?.Invoke() ?? 0);
        }
    }
}
