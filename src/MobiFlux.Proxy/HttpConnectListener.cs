using System.Net.Sockets;
using System.Text;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Proxy;

internal sealed class HttpConnectListener(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer, ProxyTransportOptions options)
    : ProxyListener(endpoint, routes, tunnels, observer, options)
{
    protected override async Task HandleClientAsync(NetworkStream client, CancellationToken cancellationToken)
    {
        using var handshake = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshake.CancelAfter(Options.ConnectTimeout);
        var request = await ReadHeaderAsync(client, handshake.Token);
        if (Endpoint.RequiresAuthentication && !Authenticate(request))
        {
            await client.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 407 Proxy Authentication Required\r\nProxy-Authenticate: Basic realm=\"MobiFlux\", charset=\"UTF-8\"\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"), handshake.Token);
            return;
        }
        var firstLine = request.Split("\r\n", 2, StringSplitOptions.None)[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (firstLine.Length != 3 || firstLine[2] is not ("HTTP/1.1" or "HTTP/1.0")) { await ReplyAsync(client, 400, "Invalid request", cancellationToken); return; }
        if (!string.Equals(firstLine[0], "CONNECT", StringComparison.OrdinalIgnoreCase))
        {
            await ForwardHttpAsync(client, request, firstLine, cancellationToken);
            return;
        }
        var target = firstLine[1]; var splitAt = target.LastIndexOf(':');
        if (splitAt <= 0 || !int.TryParse(target[(splitAt + 1)..], out var port) || port is < 1 or > 65535) { await ReplyAsync(client, 400, "Invalid target", cancellationToken); return; }
        var host = target[..splitAt];
        if (host.StartsWith('[') && host.EndsWith(']')) host = host[1..^1];
        if (string.IsNullOrWhiteSpace(host) || host.Any(char.IsWhiteSpace) || host.Contains('/') || host.Contains('@') || (host.Contains(':') && !System.Net.IPAddress.TryParse(host, out _)))
        { await ReplyAsync(client, 400, "Invalid target", cancellationToken); return; }
        var connected = false;
        try { await BridgeAsync(client, host, port, async token => { await ReplyAsync(client, 200, "Connection established", token); connected = true; }, cancellationToken); }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
        {
            if (!connected && !cancellationToken.IsCancellationRequested) await ReplyAsync(client, 502, "No cellular route", cancellationToken);
        }
    }
    private async Task ForwardHttpAsync(NetworkStream client, string request, string[] firstLine, CancellationToken token)
    {
        if (firstLine[0].Any(character => character is < 'A' or > 'Z') || !Uri.TryCreate(firstLine[1], UriKind.Absolute, out var uri) || uri.Scheme != "http" || !string.IsNullOrEmpty(uri.UserInfo))
        { await ReplyAsync(client, 400, "Absolute HTTP URL required", token); return; }
        var headers = request.Split("\r\n").Skip(1).Where(line => line.Length > 0).ToArray();
        if (headers.Any(line => line.IndexOf(':') <= 0 || char.IsWhiteSpace(line[0]))) { await ReplyAsync(client, 400, "Invalid header", token); return; }
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Proxy-Authorization", "Proxy-Connection", "Connection", "Host" };
        foreach (var header in headers.Where(line => line.StartsWith("Connection:", StringComparison.OrdinalIgnoreCase)))
            foreach (var nominated in header[(header.IndexOf(':') + 1)..].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) excluded.Add(nominated);
        if (headers.Any(line => line.StartsWith("Transfer-Encoding:", StringComparison.OrdinalIgnoreCase)) && headers.Any(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)))
        { await ReplyAsync(client, 400, "Ambiguous message framing", token); return; }
        var retained = headers.Where(line => !excluded.Contains(line[..line.IndexOf(':')])).ToArray();
        var headerText = $"{firstLine[0]} {uri.PathAndQuery} HTTP/1.1\r\nHost: {uri.Authority}\r\nConnection: close\r\n" + string.Join("\r\n", retained) + (retained.Length > 0 ? "\r\n" : "") + "\r\n";
        var connected = false;
        try { await BridgeAsync(client, uri.IdnHost, uri.Port, _ => { connected = true; return Task.CompletedTask; }, token, Encoding.ASCII.GetBytes(headerText)); }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
        {
            if (!connected && !token.IsCancellationRequested) await ReplyAsync(client, 502, "No cellular route", token);
        }
    }
    private bool Authenticate(string request)
    {
        var header = request.Split("\r\n").Skip(1).FirstOrDefault(line => line.StartsWith("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase));
        if (header is null) return false;
        var value = header[(header.IndexOf(':') + 1)..].Trim();
        if (!value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value[6..].Trim()));
            var colon = decoded.IndexOf(':');
            return colon > 0 && Endpoint.Authenticate(decoded[..colon], decoded[(colon + 1)..]);
        }
        catch (FormatException) { return false; }
    }
    private static async Task<string> ReadHeaderAsync(Stream stream, CancellationToken token)
    {
        var bytes = new List<byte>(); var buffer = new byte[1];
        while (bytes.Count < 16_384)
        {
            if (await stream.ReadAsync(buffer, token) == 0) throw new EndOfStreamException();
            bytes.Add(buffer[0]);
            if (bytes.Count >= 4 && bytes[^4] == 13 && bytes[^3] == 10 && bytes[^2] == 13 && bytes[^1] == 10) return Encoding.ASCII.GetString(bytes.ToArray());
        }
        throw new IOException("HTTP proxy header exceeds 16 KiB.");
    }
    private static Task ReplyAsync(Stream stream, int code, string message, CancellationToken token) => stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 {code} {message}\r\nConnection: close\r\n\r\n"), token).AsTask();
}
