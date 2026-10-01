using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;
using MobiFlux.Shared.Contracts;
using System.Threading.Channels;

namespace MobiFlux.Application.Abstractions;

public interface IDeviceRepository
{
    Task<DeviceNode?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DeviceNode?> GetByAdbSerialAsync(string adbSerial, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeviceNode>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(DeviceNode device, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
public interface IProxyRepository
{
    Task<ProxyEndpoint?> GetEndpointAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProxyEndpoint>> ListEndpointsAsync(CancellationToken cancellationToken);
    Task AddEndpointAsync(ProxyEndpoint endpoint, CancellationToken cancellationToken);
    Task<ProxyPool?> GetPoolAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProxyPool>> ListPoolsAsync(CancellationToken cancellationToken);
    Task AddPoolAsync(ProxyPool pool, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
public interface IProxyDefaults
{
    int MaxConnections { get; }
    DnsMode DnsMode { get; }
}
public interface IApiRequestLimits
{
    int MaximumPageSize { get; }
}
public sealed record ProxySessionFilter(Guid? EndpointId, Guid? DeviceId, int Page, int PageSize);
public sealed record ProxySessionPage(IReadOnlyList<ProxySession> Items, int Page, int PageSize, int TotalCount);
public sealed record ProxyTrafficSummary(int SessionCount, long BytesUp, long BytesDown, DateTimeOffset? MostRecentSessionAt);
public interface IProxySessionRepository
{
    Task AppendBatchAsync(IReadOnlyCollection<ProxySession> sessions, CancellationToken cancellationToken);
    Task<ProxySessionPage> GetPageAsync(ProxySessionFilter filter, CancellationToken cancellationToken);
    Task<ProxyTrafficSummary> GetSummaryAsync(CancellationToken cancellationToken);
}
public interface IProxySessionQueue
{
    ChannelReader<ProxySession> Reader { get; }
    bool TryEnqueue(ProxySession session);
    long DroppedCount { get; }
}
public sealed record AdbDevice(string Serial, string State, string? Model, string? Manufacturer, string? AndroidVersion);
public interface IAdbClient
{
    Task<IReadOnlyList<AdbDevice>> ListDevicesAsync(CancellationToken cancellationToken);
    Task<string> ExecuteShellAsync(string serial, string command, CancellationToken cancellationToken);
    Task<DeviceTelemetryDto> GetDeviceTelemetryAsync(string serial, CancellationToken cancellationToken);
    Task<bool> RotateDeviceIpAsync(string serial, CancellationToken cancellationToken);
}
public interface IAdbForwarder { Task ForwardAsync(string serial, int localPort, int devicePort, CancellationToken cancellationToken); }
public interface IProxyRuntime
{
    bool IsRunning(Guid endpointId);
    Task StartAsync(ProxyEndpoint endpoint, IReadOnlyList<DeviceNode> devices, IReadOnlyList<ProxyPool> pools, CancellationToken cancellationToken);
    Task StopAsync(Guid endpointId);
}
