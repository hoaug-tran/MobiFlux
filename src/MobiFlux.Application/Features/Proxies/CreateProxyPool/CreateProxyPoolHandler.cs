using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.CreateProxyPool;

public sealed class CreateProxyPoolHandler(IProxyRepository proxies, IDeviceRepository devices) : IRequestHandler<CreateProxyPoolCommand, ProxyPoolDto>
{
    public async Task<ProxyPoolDto> Handle(CreateProxyPoolCommand command, CancellationToken cancellationToken)
    {
        var knownDeviceIds = (await devices.ListAsync(cancellationToken)).Select(device => device.Id).ToHashSet();
        var missingDevice = command.Members.Select(member => member.DeviceId).FirstOrDefault(deviceId => !knownDeviceIds.Contains(deviceId));
        if (missingDevice != Guid.Empty) throw new KeyNotFoundException("A proxy-pool member device was not found.");

        var strategy = Enum.Parse<RoutingStrategy>(command.Strategy, true);
        var pool = new ProxyPool(command.Name, strategy, command.StickyTtlSeconds);
        foreach (var member in command.Members) pool.AddMember(member.DeviceId, member.Weight, member.Priority);
        await proxies.AddPoolAsync(pool, cancellationToken);
        await proxies.SaveChangesAsync(cancellationToken);
        return pool.ToDto();
    }
}
