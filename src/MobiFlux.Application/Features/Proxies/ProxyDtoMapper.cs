using MobiFlux.Domain.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies;

internal static class ProxyDtoMapper
{
    public static ProxyEndpointDto ToDto(this ProxyEndpoint endpoint, bool running) =>
        new(endpoint.Id, endpoint.Name, endpoint.BindAddress, endpoint.Port, endpoint.Protocol.ToString(), endpoint.DeviceId, endpoint.PoolId, endpoint.DnsMode.ToString(), endpoint.MaxConnections, endpoint.Enabled, running);

    public static ProxyPoolDto ToDto(this ProxyPool pool) =>
        new(pool.Id, pool.Name, pool.RoutingStrategy.ToString(), pool.StickyTtlSeconds, pool.Enabled,
            pool.Members.OrderBy(member => member.Priority).ThenBy(member => member.DeviceId)
                .Select(member => new ProxyPoolMemberDto(member.DeviceId, member.Weight, member.Priority, member.Enabled)).ToArray());
}
