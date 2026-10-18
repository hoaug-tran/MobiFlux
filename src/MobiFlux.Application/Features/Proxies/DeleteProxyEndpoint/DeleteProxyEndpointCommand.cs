using MediatR;
namespace MobiFlux.Application.Features.Proxies.DeleteProxyEndpoint;
public sealed record DeleteProxyEndpointCommand(Guid EndpointId) : IRequest;
