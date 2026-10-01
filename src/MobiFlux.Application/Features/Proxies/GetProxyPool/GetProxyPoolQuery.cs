using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyPool;

public sealed record GetProxyPoolQuery(Guid PoolId) : IRequest<ProxyPoolDto>;
