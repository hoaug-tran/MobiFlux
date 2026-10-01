using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.UpdateProxyPool;

public sealed class UpdateProxyPoolHandler(IProxyRepository proxies) : IRequestHandler<UpdateProxyPoolCommand, ProxyPoolDto>
{
    public async Task<ProxyPoolDto> Handle(UpdateProxyPoolCommand command, CancellationToken cancellationToken)
    {
        var pool = await proxies.GetPoolAsync(command.PoolId, cancellationToken)
            ?? throw new KeyNotFoundException("Proxy pool was not found.");

        var strategy = Enum.Parse<RoutingStrategy>(command.Strategy, true);
        pool.Update(strategy, command.StickyTtlSeconds, command.Enabled);
        await proxies.SaveChangesAsync(cancellationToken);
        return pool.ToDto();
    }
}
