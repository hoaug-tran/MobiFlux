using MobiFlux.Infrastructure.Routing;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure.Telemetry;

public sealed class ConnectionTrackingProxySessionObserver(IProxySessionObserver inner, CachedRouteResolver routes, RuntimeTelemetry telemetry) : IProxySessionObserver
{
    public async ValueTask<ProxySessionLease> OpenAsync(Guid endpointId, Guid deviceId, string destinationHost, int destinationPort, CancellationToken cancellationToken)
    {
        var session = await inner.OpenAsync(endpointId, deviceId, destinationHost, destinationPort, cancellationToken);
        routes.ConnectionOpened(deviceId);
        telemetry.Open(session);
        return session;
    }

    public async ValueTask CloseAsync(ProxySessionLease session, long bytesUp, long bytesDown, string reason, CancellationToken cancellationToken)
    {
        try { await inner.CloseAsync(session, bytesUp, bytesDown, reason, cancellationToken); }
        finally { routes.ConnectionClosed(session.DeviceId); telemetry.Close(session, reason); }
    }
    public void RecordTraffic(ProxySessionLease session, long bytesUp, long bytesDown) => telemetry.Record(bytesUp, bytesDown);
}
