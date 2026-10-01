using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyTrafficSummary;

public sealed class GetProxyTrafficSummaryHandler(IProxySessionRepository sessions) : IRequestHandler<GetProxyTrafficSummaryQuery, ProxyTrafficSummaryDto>
{
    public async Task<ProxyTrafficSummaryDto> Handle(GetProxyTrafficSummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await sessions.GetSummaryAsync(cancellationToken);
        return new ProxyTrafficSummaryDto(summary.SessionCount, summary.BytesUp, summary.BytesDown, summary.MostRecentSessionAt);
    }
}
