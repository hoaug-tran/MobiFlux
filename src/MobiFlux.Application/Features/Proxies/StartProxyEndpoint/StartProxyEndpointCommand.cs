using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.StartProxyEndpoint;

public sealed record StartProxyEndpointCommand(Guid EndpointId) : IRequest<ProxyEndpointDto>;
