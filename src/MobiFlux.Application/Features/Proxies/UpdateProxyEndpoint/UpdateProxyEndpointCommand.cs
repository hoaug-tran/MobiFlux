using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.UpdateProxyEndpoint;

public sealed record UpdateProxyEndpointCommand(
    Guid EndpointId,
    string Name,
    string BindAddress,
    int Port,
    string Protocol,
    Guid? DeviceId,
    Guid? PoolId,
    int MaxConnections,
    bool Enabled,
    string? Username = null,
    string? Password = null,
    string? AllowedClients = null) : IRequest<ProxyEndpointDto>;
