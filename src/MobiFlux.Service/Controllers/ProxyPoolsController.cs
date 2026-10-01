using MediatR;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Application.Features.Proxies.CreateProxyPool;
using MobiFlux.Application.Features.Proxies.GetProxyPool;
using MobiFlux.Application.Features.Proxies.GetProxyPools;
using MobiFlux.Application.Features.Proxies.UpdateProxyPool;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.Pools)]
public sealed class ProxyPoolsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProxyPoolDto>>>> Get(CancellationToken cancellationToken) =>
        Success(await sender.Send(new GetProxyPoolsQuery(), cancellationToken));

    [HttpGet("{poolId:guid}", Name = "GetProxyPool")]
    public async Task<ActionResult<ApiResponse<ProxyPoolDto>>> GetById(Guid poolId, CancellationToken cancellationToken) =>
        Success(await sender.Send(new GetProxyPoolQuery(poolId), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProxyPoolDto>>> Create(CreateProxyPoolRequest request, CancellationToken cancellationToken)
    {
        var pool = await sender.Send(new CreateProxyPoolCommand(request.Name, request.Strategy, request.StickyTtlSeconds, request.Members), cancellationToken);
        return CreatedAtRoute("GetProxyPool", new { poolId = pool.Id }, ApiResponse<ProxyPoolDto>.Ok(pool, HttpContext.TraceIdentifier));
    }

    [HttpPut("{poolId:guid}")]
    public async Task<ActionResult<ApiResponse<ProxyPoolDto>>> Update(Guid poolId, UpdateProxyPoolRequest request, CancellationToken cancellationToken) =>
        Success(await sender.Send(new UpdateProxyPoolCommand(poolId, request.Strategy, request.StickyTtlSeconds, request.Enabled), cancellationToken));
}
