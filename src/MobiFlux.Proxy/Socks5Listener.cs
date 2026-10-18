using System.Net;
using System.Net.Sockets;
using System.Text;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Proxy;

internal sealed class Socks5Listener(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer, ProxyTransportOptions options)
    : ProxyListener(endpoint, routes, tunnels, observer, options)
{
    protected override async Task HandleClientAsync(NetworkStream client, CancellationToken cancellationToken)
    {
        using var handshake = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshake.CancelAfter(Options.ConnectTimeout);
        var lifetime = cancellationToken;
        cancellationToken = handshake.Token;
        var greeting = await ReadExactlyAsync(client, 2, cancellationToken);
        if (greeting[0] != 5) return;
        var methods = await ReadExactlyAsync(client, greeting[1], cancellationToken);
        var method = Endpoint.RequiresAuthentication ? (byte)2 : (byte)0;
        if (!methods.Contains(method)) { await client.WriteAsync(new byte[] { 5, 0xFF }, cancellationToken); return; }
        await client.WriteAsync(new byte[] { 5, method }, cancellationToken);
        if (Endpoint.RequiresAuthentication)
        {
            var auth = await ReadExactlyAsync(client, 2, cancellationToken);
            if (auth[0] != 1 || auth[1] == 0) { await client.WriteAsync(new byte[] { 1, 1 }, cancellationToken); return; }
            var username = Encoding.UTF8.GetString(await ReadExactlyAsync(client, auth[1], cancellationToken));
            var passwordLength = (await ReadExactlyAsync(client, 1, cancellationToken))[0];
            var password = Encoding.UTF8.GetString(await ReadExactlyAsync(client, passwordLength, cancellationToken));
            var valid = passwordLength > 0 && Endpoint.Authenticate(username, password);
            await client.WriteAsync(new byte[] { 1, valid ? (byte)0 : (byte)1 }, cancellationToken);
            if (!valid) return;
        }
        var request = await ReadExactlyAsync(client, 4, cancellationToken);
        if (request[0] != 5 || request[2] != 0) { await ReplyAsync(client, 1, cancellationToken); return; }
        if (request[1] != 1) { await ReplyAsync(client, 7, cancellationToken); return; }
        if (request[3] is not (1 or 3 or 4)) { await ReplyAsync(client, 8, cancellationToken); return; }
        var host = await ReadHostAsync(client, request[3], cancellationToken);
        var portBytes = await ReadExactlyAsync(client, 2, cancellationToken);
        var port = (portBytes[0] << 8) | portBytes[1];
        if (string.IsNullOrWhiteSpace(host) || port == 0) { await ReplyAsync(client, 1, cancellationToken); return; }
        var connected = false;
        try { await BridgeAsync(client, host, port, async token => { await ReplyAsync(client, 0, token); connected = true; }, lifetime); }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
        {
            if (!connected && !lifetime.IsCancellationRequested) await ReplyAsync(client, 1, lifetime);
        }
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
