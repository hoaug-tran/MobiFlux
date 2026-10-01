using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyPools;

public sealed record GetProxyPoolsQuery : IRequest<IReadOnlyList<ProxyPoolDto>>;
