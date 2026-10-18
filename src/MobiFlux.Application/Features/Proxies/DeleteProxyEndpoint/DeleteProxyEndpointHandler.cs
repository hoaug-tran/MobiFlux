using MediatR;
using MobiFlux.Application.Abstractions;
namespace MobiFlux.Application.Features.Proxies.DeleteProxyEndpoint;
public sealed class DeleteProxyEndpointHandler(IProxyRepository proxies, IProxyRuntime runtime) : IRequestHandler<DeleteProxyEndpointCommand>
{
    public async Task Handle(DeleteProxyEndpointCommand command, CancellationToken cancellationToken)
    {
        var endpoint = await proxies.GetEndpointAsync(command.EndpointId, cancellationToken) ?? throw new KeyNotFoundException("Endpoint was not found.");
        await runtime.StopAsync(endpoint.Id);
        proxies.RemoveEndpoint(endpoint);
        await proxies.SaveChangesAsync(cancellationToken);
    }
}
