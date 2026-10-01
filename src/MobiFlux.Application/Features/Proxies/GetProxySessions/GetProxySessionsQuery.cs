using MediatR;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxySessions;

public sealed record GetProxySessionsQuery(Guid? EndpointId, Guid? DeviceId, int Page, int PageSize) : IRequest<PagedResult<ProxySessionDto>>;
