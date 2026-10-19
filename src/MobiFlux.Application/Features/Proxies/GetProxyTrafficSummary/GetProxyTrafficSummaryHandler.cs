using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyTrafficSummary;

public sealed class GetProxyTrafficSummaryHandler(IProxySessionRepository sessions, IRotationHistory? rotations = null) : IRequestHandler<GetProxyTrafficSummaryQuery, ProxyTrafficSummaryDto>
{
    public async Task<ProxyTrafficSummaryDto> Handle(GetProxyTrafficSummaryQuery query, CancellationToken cancellationToken)
    {
        DateTimeOffset? since = query.Days switch
        {
            0 => DateTimeOffset.UtcNow.Date,
            > 0 => DateTimeOffset.UtcNow.AddDays(-query.Days.Value),
            _ => null
        };
        var summary = await sessions.GetSummaryAsync(since, cancellationToken);
        var rotationList = rotations?.List() ?? [];
        var rotationsCount = since.HasValue
            ? rotationList.Count(r => r.ChangedAt >= since.Value)
            : rotationList.Count;
        return new ProxyTrafficSummaryDto(summary.SessionCount, summary.BytesUp, summary.BytesDown, summary.MostRecentSessionAt, summary.FailedSessions, rotationsCount);
    }
}
