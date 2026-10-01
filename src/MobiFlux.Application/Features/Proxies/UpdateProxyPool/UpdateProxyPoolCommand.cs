using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.UpdateProxyPool;

public sealed record UpdateProxyPoolCommand(Guid PoolId, string Strategy, int StickyTtlSeconds, bool Enabled) : IRequest<ProxyPoolDto>;
