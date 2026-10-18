using System.Net;
using MobiFlux.Application.Abstractions;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure.Telemetry;

public sealed class DeviceEgressProbe(IDeviceTunnelFactory tunnels) : IDeviceEgressProbe
{
    public async Task<string> GetIpAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(7));
        using var handler = new SocketsHttpHandler
        {
            UseProxy = false, AllowAutoRedirect = false, UseCookies = false,
            ConnectCallback = (context, token) => new ValueTask<Stream>(tunnels.ConnectAsync(deviceId, context.DnsEndPoint.Host, context.DnsEndPoint.Port, token))
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.ipify.org");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(1024, timeout.Token);
        var body = (await response.Content.ReadAsStringAsync(timeout.Token)).Trim();
        return IPAddress.TryParse(body, out var address) ? address.ToString() : throw new IOException("Invalid cellular egress response.");
    }
}
