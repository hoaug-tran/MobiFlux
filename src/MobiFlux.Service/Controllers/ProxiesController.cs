using MobiFlux.Application.Features.Proxies.TestProxyEndpoint;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Application.Features.Proxies.CreateProxyEndpoint;
using MobiFlux.Application.Features.Proxies.GetProxyEndpoints;
using MobiFlux.Application.Features.Proxies.StartProxyEndpoint;
using MobiFlux.Application.Features.Proxies.StopProxyEndpoint;
using MobiFlux.Application.Features.Proxies.UpdateProxyEndpoint;
using MobiFlux.Application.Features.Proxies.DeleteProxyEndpoint;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.Proxies)]
public sealed class ProxiesController(ISender sender) : ApiControllerBase
{
    [HttpPut("{endpointId:guid}")]
    public async Task<ActionResult<ApiResponse<ProxyEndpointDto>>> Update(Guid endpointId, UpdateProxyEndpointRequest request, CancellationToken cancellationToken) =>
        Success(await sender.Send(new UpdateProxyEndpointCommand(
            endpointId,
            request.Name,
            request.BindAddress,
            request.Port,
            request.Protocol,
            request.DeviceId,
            request.PoolId,
            request.MaxConnections,
            request.Enabled,
            request.Username,
            request.Password,
            request.AllowedClients), cancellationToken));
    [HttpDelete("{endpointId:guid}")]
    public async Task<IActionResult> Delete(Guid endpointId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProxyEndpointCommand(endpointId), cancellationToken);
        return NoContent();
    }
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProxyEndpointDto>>>> Get(CancellationToken cancellationToken) =>
        Success(await sender.Send(new GetProxyEndpointsQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProxyEndpointDto>>> Create(CreateProxyEndpointRequest request, CancellationToken cancellationToken)
    {
        var endpoint = await sender.Send(new CreateProxyEndpointCommand(request.Name, request.BindAddress, request.Port, request.Protocol, request.DeviceId, request.PoolId, request.Username, request.Password, request.AllowedClients), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = endpoint.Id }, ApiResponse<ProxyEndpointDto>.Ok(endpoint, HttpContext.TraceIdentifier));
    }

    [HttpPut("~/" + ApiRoutes.ProxyRuntime + "/{endpointId:guid}")]
    public async Task<ActionResult<ApiResponse<ProxyEndpointDto>>> Start(Guid endpointId, CancellationToken cancellationToken) =>
        Success(await sender.Send(new StartProxyEndpointCommand(endpointId), cancellationToken));

    [HttpDelete("~/" + ApiRoutes.ProxyRuntime + "/{endpointId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Stop(Guid endpointId, CancellationToken cancellationToken)
    {
        await sender.Send(new StopProxyEndpointCommand(endpointId), cancellationToken);
        return AcceptedOperation("Proxy stop requested.");
    }

    [HttpPost("{endpointId:guid}/test")]
    public async Task<ActionResult<ApiResponse<ProxyTestResultDto>>> TestEndpoint(Guid endpointId, [FromBody] ProxyProbeCredentials? credentials, CancellationToken cancellationToken) =>
        Success(await sender.Send(new TestProxyEndpointCommand(endpointId, Username: credentials?.Username, Password: credentials?.Password), cancellationToken));

    [HttpPost("test-quick")]
    public async Task<ActionResult<ApiResponse<ProxyTestResultDto>>> TestQuick([FromBody] CreateProxyEndpointRequest? request, CancellationToken cancellationToken) =>
        Success(await sender.Send(new TestProxyEndpointCommand(null, request?.BindAddress ?? "127.0.0.1", request?.Port ?? 1080, request?.Protocol ?? "Socks5"), cancellationToken));
}
