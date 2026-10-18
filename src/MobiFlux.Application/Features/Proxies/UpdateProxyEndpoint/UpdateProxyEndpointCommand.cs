using MediatR;
using MobiFlux.Shared.Contracts;
namespace MobiFlux.Application.Features.Proxies.UpdateProxyEndpoint;
public sealed record UpdateProxyEndpointCommand(Guid EndpointId, string Name, int MaxConnections, bool Enabled) : IRequest<ProxyEndpointDto>;
