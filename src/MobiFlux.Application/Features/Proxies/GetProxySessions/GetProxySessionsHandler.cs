using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxySessions;

public sealed class GetProxySessionsHandler(IProxySessionRepository sessions) : IRequestHandler<GetProxySessionsQuery, PagedResult<ProxySessionDto>>
{
    public async Task<PagedResult<ProxySessionDto>> Handle(GetProxySessionsQuery query, CancellationToken cancellationToken)
    {
        var page = await sessions.GetPageAsync(new ProxySessionFilter(query.EndpointId, query.DeviceId, query.Page, query.PageSize), cancellationToken);
        return new PagedResult<ProxySessionDto>(page.Items.Select(session => new ProxySessionDto(session.Id, session.EndpointId, session.DeviceId, session.DestinationHost, session.DestinationPort, session.StartedAt, session.EndedAt, session.BytesUp, session.BytesDown, session.CloseReason)).ToArray(), page.Page, page.PageSize, page.TotalCount);
    }
}
