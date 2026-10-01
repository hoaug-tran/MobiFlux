using MediatR;

namespace MobiFlux.Application.Features.Proxies.StopProxyEndpoint;

public sealed record StopProxyEndpointCommand(Guid EndpointId) : IRequest;
