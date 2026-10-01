using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyEndpoints;

public sealed record GetProxyEndpointsQuery : IRequest<IReadOnlyList<ProxyEndpointDto>>;
