using System.Threading.Channels;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Infrastructure.Telemetry;

public sealed class ProxySessionBuffer(int capacity) : IProxySessionQueue
{
    private readonly Channel<ProxySession> _channel = Channel.CreateBounded<ProxySession>(new BoundedChannelOptions(capacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    });
    private long _droppedCount;

    public ChannelReader<ProxySession> Reader => _channel.Reader;
    public long DroppedCount => Interlocked.Read(ref _droppedCount);

    public bool TryEnqueue(ProxySession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (_channel.Writer.TryWrite(session)) return true;
        Interlocked.Increment(ref _droppedCount);
        return false;
    }
}
