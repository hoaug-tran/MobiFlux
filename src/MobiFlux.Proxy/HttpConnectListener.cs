using System.Net.Sockets;
using System.Text;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Proxy;

internal sealed class HttpConnectListener(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer)
    : ProxyListener(endpoint, routes, tunnels, observer)
{
    protected override async Task HandleClientAsync(NetworkStream client, CancellationToken cancellationToken)
    {
        var request = await ReadHeaderAsync(client, cancellationToken);
        var firstLine = request.Split("\r\n", 2, StringSplitOptions.None)[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (firstLine.Length != 3 || !string.Equals(firstLine[0], "CONNECT", StringComparison.OrdinalIgnoreCase)) { await ReplyAsync(client, 405, "CONNECT required", cancellationToken); return; }
        var target = firstLine[1]; var splitAt = target.LastIndexOf(':');
        if (splitAt <= 0 || !int.TryParse(target[(splitAt + 1)..], out var port) || port is < 1 or > 65535) { await ReplyAsync(client, 400, "Invalid target", cancellationToken); return; }
        try { await BridgeAsync(client, target[..splitAt], port, token => ReplyAsync(client, 200, "Connection established", token), cancellationToken); }
        catch { await ReplyAsync(client, 502, "No cellular route", cancellationToken); }
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
