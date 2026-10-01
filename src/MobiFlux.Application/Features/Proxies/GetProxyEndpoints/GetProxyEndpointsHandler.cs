using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyEndpoints;

public sealed class GetProxyEndpointsHandler(IProxyRepository proxies) : IRequestHandler<GetProxyEndpointsQuery, IReadOnlyList<ProxyEndpointDto>>
{
    public async Task<IReadOnlyList<ProxyEndpointDto>> Handle(GetProxyEndpointsQuery query, CancellationToken cancellationToken) =>
        (await proxies.ListEndpointsAsync(cancellationToken)).Select(endpoint => endpoint.ToDto(false)).ToArray();
}
