using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Application.Features.Proxies;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.CreateProxyEndpoint;

public sealed class CreateProxyEndpointHandler(IProxyRepository proxies, IDeviceRepository devices, IProxyDefaults defaults) : IRequestHandler<CreateProxyEndpointCommand, ProxyEndpointDto>
{
    public async Task<ProxyEndpointDto> Handle(CreateProxyEndpointCommand command, CancellationToken cancellationToken)
    {
        var protocol = Enum.Parse<ProxyProtocol>(command.Protocol, true);
        if ((await proxies.ListEndpointsAsync(cancellationToken)).Any(endpoint => endpoint.Port == command.Port && (endpoint.BindAddress == command.BindAddress || endpoint.BindAddress is "0.0.0.0" or "::" || command.BindAddress is "0.0.0.0" or "::"))) throw new InvalidOperationException("The proxy port is already configured on this interface.");
        if (command.DeviceId is { } deviceId && await devices.GetAsync(deviceId, cancellationToken) is null) throw new KeyNotFoundException("Proxy endpoint device was not found.");
        if (command.PoolId is { } poolId && await proxies.GetPoolAsync(poolId, cancellationToken) is null) throw new KeyNotFoundException("Proxy endpoint pool was not found.");
        var endpoint = new ProxyEndpoint(command.Name, command.BindAddress, command.Port, protocol, command.DeviceId, command.PoolId, defaults.DnsMode, defaults.MaxConnections);
        endpoint.ConfigureAccess(command.Username, command.Password, command.AllowedClients);
        await proxies.AddEndpointAsync(endpoint, cancellationToken); await proxies.SaveChangesAsync(cancellationToken);
        return endpoint.ToDto(false);
    }
}
