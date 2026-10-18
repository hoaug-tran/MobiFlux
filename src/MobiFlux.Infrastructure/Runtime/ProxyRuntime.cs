using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Infrastructure.Routing;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure.Runtime;

public sealed class ProxyRuntime(ProxyGateway gateway, CachedRouteResolver routes) : IProxyRuntime
{
    public void RefreshRoutes(IReadOnlyList<DeviceNode> devices, IReadOnlyList<ProxyPool> pools) => routes.Refresh(devices, pools);
    public bool IsRunning(Guid endpointId) => gateway.IsRunning(endpointId);
    public async Task StartAsync(ProxyEndpoint endpoint, IReadOnlyList<DeviceNode> devices, IReadOnlyList<ProxyPool> pools, CancellationToken cancellationToken)
    {
        routes.Refresh(devices, pools); await gateway.StartAsync(endpoint, cancellationToken);
    }
    public Task StopAsync(Guid endpointId) => gateway.StopAsync(endpointId);
}
