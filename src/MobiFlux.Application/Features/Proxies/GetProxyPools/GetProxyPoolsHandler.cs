using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyPools;

public sealed class GetProxyPoolsHandler(IProxyRepository proxies) : IRequestHandler<GetProxyPoolsQuery, IReadOnlyList<ProxyPoolDto>>
{
    public async Task<IReadOnlyList<ProxyPoolDto>> Handle(GetProxyPoolsQuery query, CancellationToken cancellationToken) =>
        (await proxies.ListPoolsAsync(cancellationToken)).Select(pool => pool.ToDto()).ToArray();
}
