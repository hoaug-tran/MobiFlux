using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Proxies;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure.Telemetry;

public sealed class BufferedProxySessionObserver(IProxySessionQueue queue, TimeProvider timeProvider) : IProxySessionObserver
{
    public ValueTask<ProxySessionLease> OpenAsync(Guid endpointId, Guid deviceId, string destinationHost, int destinationPort, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new ProxySessionLease(Guid.NewGuid(), endpointId, deviceId, destinationHost, destinationPort, timeProvider.GetUtcNow()));

    public ValueTask CloseAsync(ProxySessionLease session, long bytesUp, long bytesDown, string reason, CancellationToken cancellationToken)
    {
        var completed = new ProxySession(session.Id, session.EndpointId, session.DeviceId, session.DestinationHost, session.DestinationPort, session.StartedAt, timeProvider.GetUtcNow(), bytesUp, bytesDown, reason);
        queue.TryEnqueue(completed);
        return ValueTask.CompletedTask;
    }
}
