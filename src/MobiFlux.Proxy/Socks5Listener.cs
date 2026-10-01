using System.Net;
using System.Net.Sockets;
using System.Text;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Proxy;

internal sealed class Socks5Listener(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer)
    : ProxyListener(endpoint, routes, tunnels, observer)
{
    protected override async Task HandleClientAsync(NetworkStream client, CancellationToken cancellationToken)
    {
        var greeting = await ReadExactlyAsync(client, 2, cancellationToken);
        if (greeting[0] != 5) return;
        var methods = await ReadExactlyAsync(client, greeting[1], cancellationToken);
        if (!methods.Contains((byte)0)) { await client.WriteAsync(new byte[] { 5, 0xFF }, cancellationToken); return; }
        await client.WriteAsync(new byte[] { 5, 0 }, cancellationToken);
        var request = await ReadExactlyAsync(client, 4, cancellationToken);
        if (request[0] != 5 || request[1] != 1) { await ReplyAsync(client, 7, cancellationToken); return; }
        var host = await ReadHostAsync(client, request[3], cancellationToken);
        var portBytes = await ReadExactlyAsync(client, 2, cancellationToken);
        var port = (portBytes[0] << 8) | portBytes[1];
        try { await BridgeAsync(client, host, port, token => ReplyAsync(client, 0, token), cancellationToken); }
        catch { await ReplyAsync(client, 1, cancellationToken); return; }
    }
    private static async Task<string> ReadHostAsync(NetworkStream stream, byte addressType, CancellationToken token) => addressType switch
    {
        1 => new IPAddress(await ReadExactlyAsync(stream, 4, token)).ToString(),
        4 => new IPAddress(await ReadExactlyAsync(stream, 16, token)).ToString(),
        3 => Encoding.ASCII.GetString(await ReadExactlyAsync(stream, (await ReadExactlyAsync(stream, 1, token))[0], token)),
        _ => throw new IOException("Unsupported SOCKS address type.")
    };
    private static Task ReplyAsync(NetworkStream stream, byte status, CancellationToken token) => stream.WriteAsync(new byte[] { 5, status, 0, 1, 0, 0, 0, 0, 0, 0 }, token).AsTask();
    internal static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken token)
    {
        var buffer = new byte[count]; var read = 0;
        while (read < count) { var current = await stream.ReadAsync(buffer.AsMemory(read), token); if (current == 0) throw new EndOfStreamException(); read += current; }
        return buffer;
    }
}
