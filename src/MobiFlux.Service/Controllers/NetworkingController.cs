using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.Networking)]
public sealed class NetworkingController : ApiControllerBase
{
    [HttpGet("netstat")]
    public ActionResult<ApiResponse<IReadOnlyList<NetstatConnectionDto>>> GetNetstat()
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnections = properties.GetActiveTcpConnections();
        var list = new List<NetstatConnectionDto>();

        foreach (var conn in tcpConnections.Take(150))
        {
            list.Add(new NetstatConnectionDto(
                conn.LocalEndPoint.Address.ToString(),
                conn.LocalEndPoint.Port,
                conn.RemoteEndPoint.Address.ToString(),
                conn.RemoteEndPoint.Port,
                conn.State.ToString()
            ));
        }

        var listeners = properties.GetActiveTcpListeners();
        foreach (var listener in listeners.Take(50))
        {
            list.Add(new NetstatConnectionDto(
                listener.Address.ToString(),
                listener.Port,
                "0.0.0.0",
                0,
                "Listen"
            ));
        }

        return Success<IReadOnlyList<NetstatConnectionDto>>(list);
    }

    [HttpGet("interfaces")]
    public ActionResult<ApiResponse<IReadOnlyList<NetworkInterfaceDto>>> GetInterfaces()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        var list = new List<NetworkInterfaceDto>();

        foreach (var ni in interfaces)
        {
            var ipProps = ni.GetIPProperties();
            var unicast = ipProps.UnicastAddresses
                .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork || x.Address.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(x => x.Address.ToString());
            var ips = string.Join(", ", unicast);

            var stats = ni.GetIPv4Statistics();

            list.Add(new NetworkInterfaceDto(
                ni.Id,
                ni.Name,
                ni.Description,
                ni.OperationalStatus.ToString(),
                ni.NetworkInterfaceType.ToString(),
                ni.Speed > 0 ? ni.Speed / 1_000_000 : 0,
                ips,
                stats.BytesReceived,
                stats.BytesSent
            ));
        }

        return Success<IReadOnlyList<NetworkInterfaceDto>>(list);
    }

    [HttpPost("ping")]
    public async Task<ActionResult<ApiResponse<PingResultDto>>> PingHost([FromBody] PingRequest request, CancellationToken cancellationToken)
    {
        var host = string.IsNullOrWhiteSpace(request.Host) ? "1.1.1.1" : request.Host.Trim();
        try
        {
            using var pinger = new Ping();
            var reply = await pinger.SendPingAsync(host, Math.Clamp(request.TimeoutMs, 500, 10000));
            var ipStr = reply.Address?.ToString() ?? host;
            var success = reply.Status == IPStatus.Success;

            return Success(new PingResultDto(
                host,
                ipStr,
                success,
                reply.RoundtripTime,
                reply.Options?.Ttl ?? 64,
                reply.Status.ToString()
            ));
        }
        catch (Exception ex)
        {
            return Success(new PingResultDto(
                host,
                "Unknown",
                false,
                0,
                0,
                ex.Message
            ));
        }
    }
}
