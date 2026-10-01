using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.StartProxyEndpoint;

public sealed class StartProxyEndpointHandler(IProxyRepository proxies, IDeviceRepository devices, IProxyRuntime runtime) : IRequestHandler<StartProxyEndpointCommand, ProxyEndpointDto>
{
    public async Task<ProxyEndpointDto> Handle(StartProxyEndpointCommand command, CancellationToken cancellationToken)
    {
        var endpoint = await proxies.GetEndpointAsync(command.EndpointId, cancellationToken) ?? throw new KeyNotFoundException("Proxy endpoint was not found.");
        await runtime.StartAsync(endpoint, await devices.ListAsync(cancellationToken), await proxies.ListPoolsAsync(cancellationToken), cancellationToken);
        return endpoint.ToDto(true);
    }
}
