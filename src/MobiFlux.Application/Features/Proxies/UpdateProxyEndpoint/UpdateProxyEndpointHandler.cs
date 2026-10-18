using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;
namespace MobiFlux.Application.Features.Proxies.UpdateProxyEndpoint;
public sealed class UpdateProxyEndpointHandler(IProxyRepository proxies, IDeviceRepository devices, IProxyRuntime runtime) : IRequestHandler<UpdateProxyEndpointCommand, ProxyEndpointDto>
{
    public async Task<ProxyEndpointDto> Handle(UpdateProxyEndpointCommand command, CancellationToken cancellationToken)
    {
        var endpoint = await proxies.GetEndpointAsync(command.EndpointId, cancellationToken) ?? throw new KeyNotFoundException("Endpoint was not found.");
        var running = runtime.IsRunning(endpoint.Id);
        await runtime.StopAsync(endpoint.Id);
        endpoint.Update(command.Name, command.MaxConnections, command.Enabled);
        await proxies.SaveChangesAsync(cancellationToken);
        if (running && endpoint.Enabled) await runtime.StartAsync(endpoint, await devices.ListAsync(cancellationToken), await proxies.ListPoolsAsync(cancellationToken), cancellationToken);
        return endpoint.ToDto(runtime.IsRunning(endpoint.Id));
    }
}
