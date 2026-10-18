using System.Net;
using System.Net.Sockets;
using System.Text;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Proxy;
using MobiFlux.Infrastructure.Telemetry;
using MobiFlux.Infrastructure.Routing;

internal static class ProxyFlowChecks
{
    public static async Task RunAsync(List<string> failures)
    {
        foreach (var protocol in new[] { ProxyProtocol.Socks5, ProxyProtocol.HttpConnect })
        foreach (var authenticated in new[] { false, true })
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var token = timeout.Token;
            using var target = new TcpListener(IPAddress.Loopback, 0);
            target.Start();
            var targetPort = ((IPEndPoint)target.LocalEndpoint).Port;
            using var reservation = new TcpListener(IPAddress.Loopback, 0);
            reservation.Start();
            var proxyPort = ((IPEndPoint)reservation.LocalEndpoint).Port;
            reservation.Stop();
            var endpoint = new ProxyEndpoint("flow", authenticated ? "0.0.0.0" : "127.0.0.1", proxyPort, protocol, Guid.NewGuid(), null, DnsMode.RemoteOnly, 1);
            if (authenticated) endpoint.ConfigureAccess("test-user", "test-password-12345", "127.0.0.1");
            var observer = new RecordingObserver();
            var telemetry = new RuntimeTelemetry(TimeProvider.System, 60);
            var tracker = new ConnectionTrackingProxySessionObserver(observer, new CachedRouteResolver(TimeProvider.System, 100), telemetry);
            await using var gateway = new ProxyGateway(new TestRoutes(), new TestTunnel(targetPort), tracker);
            try
            {
                await gateway.StartAsync(endpoint, token);
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, proxyPort, token);
                var stream = client.GetStream();
                if (protocol == ProxyProtocol.Socks5)
                {
                    await stream.WriteAsync(new byte[] { 5, 1, authenticated ? (byte)2 : (byte)0 }, token);
                    var method = new byte[2];
                    await stream.ReadExactlyAsync(method, token);
                    if (method[1] != (authenticated ? 2 : 0)) throw new IOException("Incorrect auth method");
                    if (authenticated)
                    {
                        var user = Encoding.UTF8.GetBytes("test-user"); var password = Encoding.UTF8.GetBytes("test-password-12345");
                        await stream.WriteAsync(new byte[] { 1, (byte)user.Length }.Concat(user).Append((byte)password.Length).Concat(password).ToArray(), token);
                        var authReply = new byte[2]; await stream.ReadExactlyAsync(authReply, token);
                        if (authReply[1] != 0) throw new IOException("Authentication failed");
                    }
                    await stream.WriteAsync(new byte[] { 5, 1, 0, 3, 7, 101, 120, 97, 109, 112, 108, 101, 0, 80 }, token);
                    var reply = new byte[10];
                    await stream.ReadExactlyAsync(reply, token);
                    if (reply[1] != 0) throw new IOException("SOCKS connect failed");
                }
                else
                {
                    var authHeader = authenticated ? "Proxy-Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("test-user:test-password-12345")) + "\r\n" : "";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes("CONNECT example:80 HTTP/1.1\r\nHost: example:80\r\n" + authHeader + "\r\n"), token);
                    var header = new StringBuilder();
                    var one = new byte[1];
                    while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                    {
                        await stream.ReadExactlyAsync(one, token);
                        header.Append((char)one[0]);
                    }
                    if (!header.ToString().StartsWith("HTTP/1.1 200", StringComparison.Ordinal)) throw new IOException("CONNECT failed");
                }
                using var remote = await target.AcceptTcpClientAsync(token);
                if (telemetry.Sample().ActiveConnections != 1) failures.Add("live telemetry must include active tunnel before it closes");
                using (var excess = new TcpClient())
                {
                    await excess.ConnectAsync(IPAddress.Loopback, proxyPort, token);
                    try
                    {
                        if (await excess.GetStream().ReadAsync(new byte[1], token) != 0) failures.Add($"{protocol}: connection limit not enforced");
                    }
                    catch (IOException) { }
                }
                var response = Task.Run(async () =>
                {
                    var received = new byte[3];
                    await remote.GetStream().ReadExactlyAsync(received, token);
                    var eof = await remote.GetStream().ReadAsync(new byte[1], token);
                    if (eof != 0 || Encoding.ASCII.GetString(received) != "abc") throw new IOException("upstream EOF missing");
                    await remote.GetStream().WriteAsync("reply"u8.ToArray(), token);
                    remote.Client.Shutdown(SocketShutdown.Send);
                }, token);
                await stream.WriteAsync("abc"u8.ToArray(), token);
                client.Client.Shutdown(SocketShutdown.Send);
                var result = new byte[5];
                await stream.ReadExactlyAsync(result, token);
                if (await stream.ReadAsync(new byte[1], token) != 0) failures.Add($"{protocol}: protocol bytes appended after tunnel response");
                await response;
                if (Encoding.ASCII.GetString(result) != "reply") failures.Add($"{protocol}: truncated half-close response");
                await gateway.StopAsync(endpoint.Id);
                if (observer.Up != 3 || observer.Down != 5 || observer.Closed != 1) failures.Add($"{protocol}: incorrect session accounting {observer.Up}/{observer.Down}/{observer.Closed}");
                var measured = telemetry.Sample();
                if (measured.BytesUp != 3 || measured.BytesDown != 5 || measured.ActiveConnections != 0 || measured.CompletedSessions != 1) failures.Add($"{protocol}: realtime counters differ from actual transferred bytes");
            }
            catch (Exception exception) { failures.Add($"{protocol}: half-close flow failed ({exception.GetType().Name})"); }
        }
        await CheckMissingRouteAsync(failures);
        await CheckHandshakeDeadlineAsync(failures);
        await CheckAuthenticationRequiredAsync(failures);
        await CheckPlainHttpAsync(failures);
    }
    private static async Task CheckPlainHttpAsync(List<string> failures)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = timeout.Token;
        using var target = new TcpListener(IPAddress.Loopback, 0); target.Start();
        using var reservation = new TcpListener(IPAddress.Loopback, 0); reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        var endpoint = new ProxyEndpoint("http", "127.0.0.1", port, ProxyProtocol.HttpConnect, Guid.NewGuid(), null, DnsMode.RemoteOnly, 2);
        endpoint.ConfigureAccess("test-user", "test-password-12345", null);
        await using var gateway = new ProxyGateway(new TestRoutes(), new TestTunnel(((IPEndPoint)target.LocalEndpoint).Port), new RecordingObserver());
        await gateway.StartAsync(endpoint, token);
        using var client = new TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port, token);
        var clientStream = client.GetStream();
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-user:test-password-12345"));
        await clientStream.WriteAsync(Encoding.ASCII.GetBytes($"GET http://example.test/path?x=1 HTTP/1.1\r\nHost: example.test\r\nProxy-Authorization: Basic {auth}\r\n\r\n"), token);
        client.Client.Shutdown(SocketShutdown.Send);
        using var remote = await target.AcceptTcpClientAsync(token);
        var forwarded = new StringBuilder(); var one = new byte[1];
        while (!forwarded.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
        {
            await remote.GetStream().ReadExactlyAsync(one, token); forwarded.Append((char)one[0]);
        }
        if (!forwarded.ToString().StartsWith("GET /path?x=1 HTTP/1.1", StringComparison.Ordinal) || forwarded.ToString().Contains("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)) failures.Add("HTTP forwarding must rewrite absolute URL and strip proxy credentials");
        await remote.GetStream().WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\nConnection: close\r\n\r\nhello"), token);
        remote.Client.Shutdown(SocketShutdown.Send);
        using var reader = new StreamReader(clientStream, Encoding.ASCII);
        var response = await reader.ReadToEndAsync(token);
        if (!response.EndsWith("hello", StringComparison.Ordinal)) failures.Add("plain HTTP response did not reach client");
    }
    private static async Task CheckAuthenticationRequiredAsync(List<string> failures)
    {
        foreach (var protocol in new[] { ProxyProtocol.Socks5, ProxyProtocol.HttpConnect })
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var reservation = new TcpListener(IPAddress.Loopback, 0); reservation.Start();
            var port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
            var endpoint = new ProxyEndpoint("auth", "0.0.0.0", port, protocol, Guid.NewGuid(), null, DnsMode.RemoteOnly, 4);
            endpoint.ConfigureAccess("test-user", "test-password-12345", "127.0.0.1");
            var tunnels = new ForbiddenTunnel();
            await using var gateway = new ProxyGateway(new MissingRoutes(), tunnels, new RecordingObserver());
            await gateway.StartAsync(endpoint, timeout.Token);
            using var client = new TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
            var stream = client.GetStream();
            if (protocol == ProxyProtocol.Socks5)
            {
                await stream.WriteAsync(new byte[] { 5, 1, 0 }, timeout.Token);
                var reply = new byte[2]; await stream.ReadExactlyAsync(reply, timeout.Token);
                if (reply[1] != 255) failures.Add("authenticated SOCKS endpoint accepted anonymous negotiation");
            }
            else
            {
                await stream.WriteAsync(Encoding.ASCII.GetBytes("CONNECT example:443 HTTP/1.1\r\nProxy-Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("test-user:wrong-password")) + "\r\n\r\n"), timeout.Token);
                var reply = new byte[12]; await stream.ReadExactlyAsync(reply, timeout.Token);
                if (!Encoding.ASCII.GetString(reply).StartsWith("HTTP/1.1 407", StringComparison.Ordinal)) failures.Add("authenticated HTTP endpoint accepted incorrect password");
            }
            if (tunnels.Calls != 0) failures.Add("authentication failure must not open tunnel");
        }
    }

    private static async Task CheckHandshakeDeadlineAsync(List<string> failures)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var endpoint = new ProxyEndpoint("deadline", "127.0.0.1", port, ProxyProtocol.Socks5, Guid.NewGuid(), null, DnsMode.RemoteOnly, 1);
        await using var gateway = new ProxyGateway(new MissingRoutes(), new ForbiddenTunnel(), new RecordingObserver(), new(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
        await gateway.StartAsync(endpoint, timeout.Token);
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        if (await client.GetStream().ReadAsync(new byte[1], timeout.Token) != 0) failures.Add("incomplete handshake must expire");
        using var waiting = new TcpClient();
        await waiting.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        await waiting.GetStream().WriteAsync(new byte[] { 5, 1, 0 }, timeout.Token);
        await waiting.GetStream().ReadExactlyAsync(new byte[2], timeout.Token);
        await gateway.StopAsync(endpoint.Id).WaitAsync(timeout.Token);
        if (gateway.IsRunning(endpoint.Id)) failures.Add("stopped gateway still reported running");
        if (await waiting.GetStream().ReadAsync(new byte[1], timeout.Token) != 0) failures.Add("stop must close pending handshakes");
    }

    private static async Task CheckMissingRouteAsync(List<string> failures)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var endpoint = new ProxyEndpoint("unavailable", "127.0.0.1", port, ProxyProtocol.Socks5, Guid.NewGuid(), null, DnsMode.RemoteOnly, 1);
        var tunnels = new ForbiddenTunnel();
        await using var gateway = new ProxyGateway(new MissingRoutes(), tunnels, new RecordingObserver());
        await gateway.StartAsync(endpoint, timeout.Token);
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        var stream = client.GetStream();
        await stream.WriteAsync(new byte[] { 5, 1, 0 }, timeout.Token);
        await stream.ReadExactlyAsync(new byte[2], timeout.Token);
        await stream.WriteAsync(new byte[] { 5, 1, 0, 1, 1, 1, 1, 1, 0, 80 }, timeout.Token);
        var reply = new byte[10];
        await stream.ReadExactlyAsync(reply, timeout.Token);
        if (reply[1] == 0 || tunnels.Calls != 0) failures.Add("missing cellular route must fail before opening a tunnel");
    }

    private sealed class MissingRoutes : IRouteResolver
    {
        public Task<RouteDecision?> ResolveAsync(ProxyEndpoint endpoint, string key, CancellationToken token) => Task.FromResult<RouteDecision?>(null);
    }
    private sealed class ForbiddenTunnel : IDeviceTunnelFactory
    {
        public int Calls;
        public Task<Stream> ConnectAsync(Guid id, string host, int port, CancellationToken token) { Calls++; throw new IOException("must not connect"); }
    }

    private sealed class TestRoutes : IRouteResolver
    {
        public Task<RouteDecision?> ResolveAsync(ProxyEndpoint endpoint, string key, CancellationToken token) => Task.FromResult<RouteDecision?>(new(endpoint.DeviceId!.Value, "test"));
    }
    private sealed class TestTunnel(int port) : IDeviceTunnelFactory
    {
        public async Task<Stream> ConnectAsync(Guid id, string host, int destinationPort, CancellationToken token)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(IPAddress.Loopback, port, token);
            return new NetworkStream(socket, ownsSocket: true);
        }
    }
    private sealed class RecordingObserver : IProxySessionObserver
    {
        public long Up; public long Down; public int Closed;
        public ValueTask<ProxySessionLease> OpenAsync(Guid endpoint, Guid device, string host, int port, CancellationToken token) => ValueTask.FromResult(new ProxySessionLease(Guid.NewGuid(), endpoint, device, host, port, DateTimeOffset.UtcNow));
        public ValueTask CloseAsync(ProxySessionLease session, long up, long down, string reason, CancellationToken token)
        {
            Up = up; Down = down; Interlocked.Increment(ref Closed); return ValueTask.CompletedTask;
        }
    }
}
