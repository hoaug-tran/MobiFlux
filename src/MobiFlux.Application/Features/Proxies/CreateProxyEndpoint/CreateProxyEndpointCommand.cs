using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.CreateProxyEndpoint;

public sealed record CreateProxyEndpointCommand(string Name, string BindAddress, int Port, string Protocol, Guid? DeviceId, Guid? PoolId) : IRequest<ProxyEndpointDto>;
