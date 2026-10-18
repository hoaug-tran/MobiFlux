using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.TestProxyEndpoint;

public sealed class TestProxyEndpointHandler(IProxyRepository proxies, IProxyConnectivityProbe probe) : IRequestHandler<TestProxyEndpointCommand, ProxyTestResultDto>
{
    public async Task<ProxyTestResultDto> Handle(TestProxyEndpointCommand command, CancellationToken cancellationToken)
    {
        if (command.EndpointId is { } id)
        {
            var endpoint = await proxies.GetEndpointAsync(id, cancellationToken) ?? throw new KeyNotFoundException("Proxy endpoint was not found.");
            return await probe.TestAsync(id, endpoint.BindAddress, endpoint.Port, endpoint.Protocol, cancellationToken, command.Username, command.Password);
        }
        return await probe.TestAsync(Guid.Empty, command.BindAddress, command.Port, Enum.Parse<ProxyProtocol>(command.Protocol, true), cancellationToken);
    }
}
