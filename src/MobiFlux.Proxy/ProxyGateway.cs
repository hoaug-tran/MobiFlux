using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Proxy;

public sealed class ProxyGateway(IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, ProxyListener> _listeners = new();
    public bool IsRunning(Guid endpointId) => _listeners.ContainsKey(endpointId);
    public async Task StartAsync(ProxyEndpoint endpoint, CancellationToken cancellationToken)
    {
        if (!endpoint.Enabled) throw new InvalidOperationException("Disabled endpoints cannot be started.");
        var listener = ProxyListener.Create(endpoint, routes, tunnels, observer);
        if (!_listeners.TryAdd(endpoint.Id, listener)) return;
        try { await listener.StartAsync(cancellationToken); }
        catch { _listeners.TryRemove(endpoint.Id, out _); await listener.DisposeAsync(); throw; }
    }
    public async Task StopAsync(Guid endpointId)
    {
        if (_listeners.TryRemove(endpointId, out var listener)) await listener.DisposeAsync();
    }
    public async ValueTask DisposeAsync()
    {
        foreach (var id in _listeners.Keys) await StopAsync(id);
    }
}

internal abstract class ProxyListener : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _stop = new();
    private Task? _acceptLoop;
    protected ProxyListener(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer)
    {
        Endpoint = endpoint; Routes = routes; Tunnels = tunnels; Observer = observer;
        if (!IPAddress.TryParse(endpoint.BindAddress, out var address)) throw new ArgumentException("Bind address must be an IP address.", nameof(endpoint));
        if (!IPAddress.IsLoopback(address)) throw new InvalidOperationException("Non-loopback listeners require an explicit LAN security policy.");
        _listener = new TcpListener(address, endpoint.Port);
    }
    protected ProxyEndpoint Endpoint { get; }
    protected IRouteResolver Routes { get; }
    protected IDeviceTunnelFactory Tunnels { get; }
    protected IProxySessionObserver Observer { get; }
    public static ProxyListener Create(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer) => endpoint.Protocol switch
    {
        ProxyProtocol.Socks5 => new Socks5Listener(endpoint, routes, tunnels, observer),
        ProxyProtocol.HttpConnect => new HttpConnectListener(endpoint, routes, tunnels, observer),
        _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
    };
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start(Endpoint.MaxConnections); _acceptLoop = AcceptLoopAsync(_stop.Token); return Task.CompletedTask;
    }
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientSafelyAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }
    private async Task HandleClientSafelyAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try { await HandleClientAsync(client.GetStream(), cancellationToken); }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (SocketException) { }
        }
    }
    protected abstract Task HandleClientAsync(NetworkStream client, CancellationToken cancellationToken);
    protected async Task BridgeAsync(NetworkStream client, string host, int port, Func<CancellationToken, Task> onConnected, CancellationToken cancellationToken)
    {
        var route = await Routes.ResolveAsync(Endpoint, client.Socket.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString("N"), cancellationToken);
        if (route is null) throw new IOException("No eligible cellular device is available.");
        await using var device = await Tunnels.ConnectAsync(route.DeviceId, host, port, cancellationToken);
        var session = await Observer.OpenAsync(Endpoint.Id, route.DeviceId, host, port, cancellationToken);
        await onConnected(cancellationToken);
        var up = new CountingStream(client); var down = new CountingStream(device);
        var reason = "completed";
        try { await Task.WhenAny(up.CopyToAsync(device, cancellationToken), down.CopyToAsync(client, cancellationToken)); }
        catch (OperationCanceledException) { reason = "cancelled"; throw; }
        catch (Exception) { reason = "transport-failure"; throw; }
        finally { await Observer.CloseAsync(session, up.BytesWritten, down.BytesWritten, reason, CancellationToken.None); }
    }
    public async ValueTask DisposeAsync()
    {
        _stop.Cancel(); _listener.Stop();
        if (_acceptLoop is not null) await _acceptLoop;
        _stop.Dispose();
    }
}

internal sealed class CountingStream(Stream inner) : Stream
{
    public long BytesWritten { get; private set; }
    public override bool CanRead => inner.CanRead; public override bool CanSeek => false; public override bool CanWrite => inner.CanWrite;
    public override long Length => throw new NotSupportedException(); public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush(); public override Task FlushAsync(CancellationToken token) => inner.FlushAsync(token);
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) => inner.ReadAsync(buffer, token);
    public override void Write(byte[] buffer, int offset, int count) { BytesWritten += count; inner.Write(buffer, offset, count); }
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default) { BytesWritten += buffer.Length; await inner.WriteAsync(buffer, token); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(); public override void SetLength(long value) => throw new NotSupportedException();
}
