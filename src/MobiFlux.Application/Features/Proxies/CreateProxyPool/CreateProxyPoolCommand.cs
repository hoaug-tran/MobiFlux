using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.CreateProxyPool;

public sealed record CreateProxyPoolCommand(string Name, string Strategy, int StickyTtlSeconds, IReadOnlyList<ProxyPoolMemberRequest> Members) : IRequest<ProxyPoolDto>;
