using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.GetProxyTrafficSummary;

public sealed record GetProxyTrafficSummaryQuery(int? Days = null) : IRequest<ProxyTrafficSummaryDto>;
