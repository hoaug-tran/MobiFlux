using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyPool;

public sealed class GetProxyPoolHandler(IProxyRepository proxies) : IRequestHandler<GetProxyPoolQuery, ProxyPoolDto>
{
    public async Task<ProxyPoolDto> Handle(GetProxyPoolQuery query, CancellationToken cancellationToken) =>
        (await proxies.GetPoolAsync(query.PoolId, cancellationToken) ?? throw new KeyNotFoundException("Proxy pool was not found.")).ToDto();
}
