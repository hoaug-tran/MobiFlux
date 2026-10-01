using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using MobiFlux.Application.Abstractions;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure.AndroidAgent;

public sealed class AgentForwardRegistry
{
    private readonly ConcurrentDictionary<Guid, int> _ports = new();
    private int _nextPort;
    public AgentForwardRegistry(int firstPort) => _nextPort = firstPort - 2;
    public int GetOrAssign(Guid deviceId) => _ports.GetOrAdd(deviceId, _ => Interlocked.Add(ref _nextPort, 2) - 2);
    public void Set(Guid deviceId, int localPort) => _ports[deviceId] = localPort;
    public bool TryGet(Guid deviceId, out int localPort) => _ports.TryGetValue(deviceId, out localPort);
}

public sealed class AgentStatusProbe(IAdbForwarder adb, AgentForwardRegistry forwards)
{
    public async Task<bool> ProbeCellularAsync(string serial, Guid deviceId, CancellationToken cancellationToken)
    {
        var tunnelPort = forwards.GetOrAssign(deviceId); var controlPort = tunnelPort + 1;
        await adb.ForwardAsync(serial, tunnelPort, 18_080, cancellationToken);
        await adb.ForwardAsync(serial, controlPort, 18_081, cancellationToken);
        forwards.Set(deviceId, tunnelPort);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await client.GetAsync($"http://127.0.0.1:{controlPort}/agent/status", cancellationToken);
        if (!response.IsSuccessStatusCode) return false;
        return (await response.Content.ReadAsStringAsync(cancellationToken)).Contains("\"cellularAvailable\":true", StringComparison.Ordinal);
    }
}

public sealed class AgentTunnelFactory(AgentForwardRegistry forwards) : IDeviceTunnelFactory
{
    public async Task<Stream> ConnectAsync(Guid deviceId, string destinationHost, int destinationPort, CancellationToken cancellationToken)
    {
        if (!forwards.TryGet(deviceId, out var port)) throw new IOException("Android agent port forward is not available.");
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync("127.0.0.1", port, cancellationToken);
            var stream = client.GetStream();
            var hostBytes = Encoding.UTF8.GetBytes(destinationHost);
            if (hostBytes.Length is 0 or > 253) throw new ArgumentException("Destination host length is invalid.", nameof(destinationHost));
            await stream.WriteAsync("MFP1"u8.ToArray(), cancellationToken);
            await stream.WriteAsync(new byte[] { (byte)hostBytes.Length }, cancellationToken);
            await stream.WriteAsync(hostBytes, cancellationToken);
            await stream.WriteAsync(new byte[] { (byte)(destinationPort >> 8), (byte)destinationPort }, cancellationToken);
            var reply = new byte[1];
            if (await stream.ReadAsync(reply, cancellationToken) != 1 || reply[0] != 0) throw new IOException("Android agent rejected the cellular tunnel request.");
            return new OwnedNetworkStream(client, stream);
        }
        catch { client.Dispose(); throw; }
    }
    private sealed class OwnedNetworkStream(TcpClient client, NetworkStream inner) : Stream
    {
        public override bool CanRead => inner.CanRead; public override bool CanSeek => false; public override bool CanWrite => inner.CanWrite; public override long Length => throw new NotSupportedException(); public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush(); public override Task FlushAsync(CancellationToken token) => inner.FlushAsync(token); public override int Read(byte[] b, int o, int c) => inner.Read(b, o, c); public override ValueTask<int> ReadAsync(Memory<byte> b, CancellationToken t = default) => inner.ReadAsync(b, t);
        public override void Write(byte[] b, int o, int c) => inner.Write(b, o, c); public override ValueTask WriteAsync(ReadOnlyMemory<byte> b, CancellationToken t = default) => inner.WriteAsync(b, t); public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException(); public override void SetLength(long v) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) { inner.Dispose(); client.Dispose(); } base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await inner.DisposeAsync(); client.Dispose(); GC.SuppressFinalize(this); }
    }
}
