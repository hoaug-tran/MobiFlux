using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.UpdateProxyPool;

public sealed class UpdateProxyPoolHandler(IProxyRepository proxies, IDeviceRepository devices, IProxyRuntime runtime) : IRequestHandler<UpdateProxyPoolCommand, ProxyPoolDto>
{
    public async Task<ProxyPoolDto> Handle(UpdateProxyPoolCommand command, CancellationToken cancellationToken)
    {
        var pool = await proxies.GetPoolAsync(command.PoolId, cancellationToken)
            ?? throw new KeyNotFoundException("Proxy pool was not found.");

        var strategy = Enum.Parse<RoutingStrategy>(command.Strategy, true);
        pool.Update(strategy, command.StickyTtlSeconds, command.Enabled);
        if (command.Members is not null)
        {
            var known = (await devices.ListAsync(cancellationToken)).Select(device => device.Id).ToHashSet();
            if (command.Members.Any(member => !known.Contains(member.DeviceId))) throw new KeyNotFoundException("Pool member device was not found.");
            var existing = pool.Members.ToDictionary(member => member.DeviceId);
            pool.ReplaceMembers(command.Members.Select(member =>
            {
                if (!existing.TryGetValue(member.DeviceId, out var stored)) return new MobiFlux.Domain.Proxies.ProxyPoolMember(pool.Id, member.DeviceId, member.Weight, member.Priority);
                stored.Update(member.Weight, member.Priority);
                return stored;
            }));
        }
        await proxies.SaveChangesAsync(cancellationToken);
        runtime.RefreshRoutes(await devices.ListAsync(cancellationToken), await proxies.ListPoolsAsync(cancellationToken));
        return pool.ToDto();
    }
}
