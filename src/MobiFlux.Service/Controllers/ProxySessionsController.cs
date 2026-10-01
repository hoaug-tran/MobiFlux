using MediatR;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Application.Features.Proxies.GetProxySessions;
using MobiFlux.Application.Features.Proxies.GetProxyTrafficSummary;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.ProxySessions)]
public sealed class ProxySessionsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProxySessionDto>>>> Get([FromQuery] Guid? endpointId, [FromQuery] Guid? deviceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        Success(await sender.Send(new GetProxySessionsQuery(endpointId, deviceId, page, pageSize), cancellationToken));

    [HttpGet("~/" + ApiRoutes.ProxyTraffic)]
    public async Task<ActionResult<ApiResponse<ProxyTrafficSummaryDto>>> GetTrafficSummary(CancellationToken cancellationToken) =>
        Success(await sender.Send(new GetProxyTrafficSummaryQuery(), cancellationToken));
}
