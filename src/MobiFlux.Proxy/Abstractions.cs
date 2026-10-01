using MobiFlux.Domain.Proxies;

namespace MobiFlux.Proxy;

public sealed record RouteDecision(Guid DeviceId, string Reason);
public sealed record ProxySessionLease(Guid Id, Guid EndpointId, Guid DeviceId, string DestinationHost, int DestinationPort, DateTimeOffset StartedAt);
public interface IRouteResolver { Task<RouteDecision?> ResolveAsync(ProxyEndpoint endpoint, string stickyKey, CancellationToken cancellationToken); }
public interface IDeviceTunnelFactory { Task<Stream> ConnectAsync(Guid deviceId, string destinationHost, int destinationPort, CancellationToken cancellationToken); }
public interface IProxySessionObserver
{
    ValueTask<ProxySessionLease> OpenAsync(Guid endpointId, Guid deviceId, string destinationHost, int destinationPort, CancellationToken cancellationToken);
    ValueTask CloseAsync(ProxySessionLease session, long bytesUp, long bytesDown, string reason, CancellationToken cancellationToken);
}
public sealed class NullProxySessionObserver : IProxySessionObserver
{
    public ValueTask<ProxySessionLease> OpenAsync(Guid endpointId, Guid deviceId, string destinationHost, int destinationPort, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new ProxySessionLease(Guid.NewGuid(), endpointId, deviceId, destinationHost, destinationPort, DateTimeOffset.MinValue));
    public ValueTask CloseAsync(ProxySessionLease session, long bytesUp, long bytesDown, string reason, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
