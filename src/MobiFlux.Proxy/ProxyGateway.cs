using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Proxy;

public sealed class ProxyGateway(IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer, ProxyTransportOptions? options = null) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, ProxyListener> _listeners = new();
    private readonly SemaphoreSlim _lifecycle = new(1);
    public int RunningCount => _listeners.Count;
    public bool IsRunning(Guid endpointId) => _listeners.ContainsKey(endpointId);
    public async Task StartAsync(ProxyEndpoint endpoint, CancellationToken cancellationToken)
    {
        await _lifecycle.WaitAsync(cancellationToken);
        try
        {
            if (!endpoint.Enabled) throw new InvalidOperationException("Disabled endpoints cannot be started.");
            if (_listeners.ContainsKey(endpoint.Id)) return;
            var listener = ProxyListener.Create(endpoint, routes, tunnels, observer, options ?? ProxyTransportOptions.Default);
            try { await listener.StartAsync(cancellationToken); _listeners[endpoint.Id] = listener; }
            catch { await listener.DisposeAsync(); throw; }
        }
        finally { _lifecycle.Release(); }
    }
    public async Task StopAsync(Guid endpointId)
    {
        await _lifecycle.WaitAsync();
        try { if (_listeners.TryRemove(endpointId, out var listener)) await listener.DisposeAsync(); }
        finally { _lifecycle.Release(); }
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
    private readonly ConcurrentDictionary<long, Task> _clients = new();
    private Task? _acceptLoop;
    private long _nextClient;
    protected ProxyListener(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer, ProxyTransportOptions options)
    {
        Endpoint = endpoint; Routes = routes; Tunnels = tunnels; Observer = observer; Options = options;
        if (!IPAddress.TryParse(endpoint.BindAddress, out var address)) throw new ArgumentException("Bind address must be an IP address.", nameof(endpoint));
        _listener = new TcpListener(address, endpoint.Port);
    }
    protected ProxyEndpoint Endpoint { get; }
    protected IRouteResolver Routes { get; }
    protected IDeviceTunnelFactory Tunnels { get; }
    protected IProxySessionObserver Observer { get; }
    protected ProxyTransportOptions Options { get; }
    public static ProxyListener Create(ProxyEndpoint endpoint, IRouteResolver routes, IDeviceTunnelFactory tunnels, IProxySessionObserver observer, ProxyTransportOptions options) => endpoint.Protocol switch
    {
        ProxyProtocol.Socks5 => new Socks5Listener(endpoint, routes, tunnels, observer, options),
        ProxyProtocol.HttpConnect => new HttpConnectListener(endpoint, routes, tunnels, observer, options),
        _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
    };
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _listener.Start(Endpoint.MaxConnections); _acceptLoop = AcceptLoopAsync(_stop.Token); return Task.CompletedTask;
    }
    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                if (client.Client.RemoteEndPoint is not IPEndPoint peer || !Endpoint.AllowsClient(peer.Address)) { client.Dispose(); continue; }
                foreach (var entry in _clients.Where(entry => entry.Value.IsCompleted)) _clients.TryRemove(entry.Key, out _);
                if (_clients.Count >= Endpoint.MaxConnections) { client.Dispose(); continue; }
                _clients[Interlocked.Increment(ref _nextClient)] = HandleClientSafelyAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { }
        catch (SocketException) when (cancellationToken.IsCancellationRequested) { }
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
    protected async Task BridgeAsync(NetworkStream client, string host, int port, Func<CancellationToken, Task> onConnected, CancellationToken cancellationToken, ReadOnlyMemory<byte> initialData = default)
    {
        using var handshake = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshake.CancelAfter(Options.ConnectTimeout);
        var stickyKey = (client.Socket.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? Guid.NewGuid().ToString("N");
        var route = await Routes.ResolveAsync(Endpoint, stickyKey, handshake.Token);
        if (route is null) throw new IOException("No eligible cellular device is available.");
        await using var device = await Tunnels.ConnectAsync(route.DeviceId, host, port, handshake.Token);
        var session = await Observer.OpenAsync(Endpoint.Id, route.DeviceId, host, port, handshake.Token);
        using var transfer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        transfer.CancelAfter(Options.IdleTimeout);
        long bytesUp = 0, bytesDown = 0;
        var reason = "completed";
        try
        {
            if (!initialData.IsEmpty) { await device.WriteAsync(initialData, handshake.Token); bytesUp += initialData.Length; Observer.RecordTraffic(session, initialData.Length, 0); }
            await onConnected(handshake.Token);
            var up = PumpAsync(client, device, count => { bytesUp += count; Observer.RecordTraffic(session, count, 0); transfer.CancelAfter(Options.IdleTimeout); }, transfer.Token);
            var down = PumpAsync(device, client, count => { bytesDown += count; Observer.RecordTraffic(session, 0, count); transfer.CancelAfter(Options.IdleTimeout); }, transfer.Token);
            try
            {
                var first = await Task.WhenAny(up, down);
                await first;
                await Task.WhenAll(up, down);
            }
            finally
            {
                await transfer.CancelAsync();
                try { await Task.WhenAll(up, down); } catch { }
            }
        }
        catch (OperationCanceledException) { reason = cancellationToken.IsCancellationRequested ? "cancelled" : "timeout"; throw; }
        catch (Exception) { reason = "transport-failure"; throw; }
        finally { await Observer.CloseAsync(session, bytesUp, bytesDown, reason, CancellationToken.None); }
    }
    private static async Task PumpAsync(Stream source, Stream destination, Action<int> count, CancellationToken token)
    {
        var buffer = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(buffer, token);
            if (read == 0) break;
            await destination.WriteAsync(buffer.AsMemory(0, read), token);
            count(read);
        }
        if (destination is NetworkStream network) network.Socket.Shutdown(SocketShutdown.Send);
        else if (destination is IHalfCloseStream tunnel) tunnel.ShutdownWrite();
    }
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync(); _listener.Stop();
        if (_acceptLoop is not null) await _acceptLoop;
        await Task.WhenAll(_clients.Values);
        _stop.Dispose();
    }
}
