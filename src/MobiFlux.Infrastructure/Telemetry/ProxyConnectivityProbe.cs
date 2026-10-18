using System.Diagnostics;
using System.Net;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Infrastructure.Telemetry;

public sealed class ProxyConnectivityProbe : IProxyConnectivityProbe
{
    public async Task<ProxyTestResultDto> TestAsync(Guid endpointId, string bindAddress, int port, ProxyProtocol protocol, CancellationToken cancellationToken, string? username = null, string? password = null)
    {
        if (!IPAddress.TryParse(bindAddress, out var address)) throw new ArgumentException("Invalid proxy bind address.", nameof(bindAddress));
        if (address.Equals(IPAddress.Any)) address = IPAddress.Loopback;
        if (address.Equals(IPAddress.IPv6Any)) address = IPAddress.IPv6Loopback;
        var scheme = protocol switch { ProxyProtocol.Socks5 => "socks5", ProxyProtocol.HttpConnect => "http", _ => throw new ArgumentOutOfRangeException(nameof(protocol)) };
        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var handler = new HttpClientHandler
        {
            Proxy = new WebProxy(new UriBuilder(scheme, address.ToString(), port).Uri) { Credentials = string.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password) },
            UseProxy = true,
            AllowAutoRedirect = false,
            UseCookies = false
        };
        using var client = new HttpClient(handler);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.ipify.org");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync(1024, timeout.Token);
            var body = (await response.Content.ReadAsStringAsync(timeout.Token)).Trim();
            if (!IPAddress.TryParse(body, out var egress)) throw new IOException("Egress check returned an invalid IP address.");
            return new(endpointId, true, (int)stopwatch.ElapsedMilliseconds, egress.ToString(), protocol.ToString(), "HTTPS qua proxy thành công; cần đối chiếu SIM để xác minh 4G.");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(endpointId, false, (int)stopwatch.ElapsedMilliseconds, string.Empty, protocol.ToString(), exception is OperationCanceledException ? "Kiểm tra proxy quá thời gian." : "Không thể hoàn tất HTTPS qua proxy.");
        }
    }
}
