using System.Diagnostics;
using System.Net.Sockets;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Application.Features.Proxies.CreateProxyEndpoint;
using MobiFlux.Application.Features.Proxies.GetProxyEndpoints;
using MobiFlux.Application.Features.Proxies.StartProxyEndpoint;
using MobiFlux.Application.Features.Proxies.StopProxyEndpoint;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.Proxies)]
public sealed class ProxiesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProxyEndpointDto>>>> Get(CancellationToken cancellationToken) =>
        Success(await sender.Send(new GetProxyEndpointsQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProxyEndpointDto>>> Create(CreateProxyEndpointRequest request, CancellationToken cancellationToken)
    {
        var endpoint = await sender.Send(new CreateProxyEndpointCommand(request.Name, request.BindAddress, request.Port, request.Protocol, request.DeviceId, request.PoolId), cancellationToken);
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
    public async Task<ActionResult<ApiResponse<ProxyTestResultDto>>> TestEndpoint(Guid endpointId, CancellationToken cancellationToken)
    {
        var endpoints = await sender.Send(new GetProxyEndpointsQuery(), cancellationToken);
        var ep = endpoints.FirstOrDefault(x => x.Id == endpointId);
        if (ep is null) return NotFound(new ApiError("ENDPOINT_NOT_FOUND", "Proxy endpoint was not found."));

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(ep.BindAddress, ep.Port, cts.Token);
            sw.Stop();
            var result = new ProxyTestResultDto(endpointId, true, (int)sw.ElapsedMilliseconds, ep.BindAddress, ep.Protocol, "Hoạt động bình thường (Operational)");
            return Success(result);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var result = new ProxyTestResultDto(endpointId, false, (int)sw.ElapsedMilliseconds, ep.BindAddress, ep.Protocol, $"Không thể kết nối cổng {ep.Port}: {ex.Message}");
            return Success(result);
        }
    }

    [HttpPost("test-quick")]
    public async Task<ActionResult<ApiResponse<ProxyTestResultDto>>> TestQuick([FromBody] CreateProxyEndpointRequest? req, CancellationToken cancellationToken)
    {
        var host = req?.BindAddress ?? "127.0.0.1";
        var port = req?.Port ?? 1080;
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, cts.Token);
            sw.Stop();
            var result = new ProxyTestResultDto(Guid.NewGuid(), true, (int)sw.ElapsedMilliseconds, host, req?.Protocol ?? "SOCKS5", "Đạt chuẩn an toàn di động (Operational)");
            return Success(result);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var result = new ProxyTestResultDto(Guid.NewGuid(), false, (int)sw.ElapsedMilliseconds, host, req?.Protocol ?? "SOCKS5", $"Chưa mở hoặc không thể kết nối: {ex.Message}");
            return Success(result);
        }
    }
}
