using MediatR;
using MobiFlux.Application.Abstractions;

namespace MobiFlux.Application.Features.Proxies.StopProxyEndpoint;

public sealed class StopProxyEndpointHandler(IProxyRuntime runtime) : IRequestHandler<StopProxyEndpointCommand>
{
    public async Task Handle(StopProxyEndpointCommand command, CancellationToken cancellationToken) => await runtime.StopAsync(command.EndpointId);
}
