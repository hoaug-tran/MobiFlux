namespace MobiFlux.Shared.Contracts;

public sealed record DeviceDto(
    Guid Id,
    string AdbSerial,
    string Name,
    bool Enabled,
    string DesiredState,
    string ActualState,
    DateTimeOffset? LastSeenAt,
    string? Carrier = null,
    string? Network = null,
    string? SignalDbm = null,
    string? Battery = null,
    string? MonthlyQuota = null,
    string? Apn = null,
    string? Iccid = null,
    string? WanIp = null
);

public sealed record RegisterDeviceRequest(string AdbSerial, string Name);
public sealed record DeviceStateRequest(bool Enabled);
public sealed record ProxyEndpointDto(Guid Id, string Name, string BindAddress, int Port, string Protocol, Guid? DeviceId, Guid? PoolId, string DnsMode, int MaxConnections, bool Enabled, bool Running);
public sealed record ProxyPoolMemberDto(Guid DeviceId, int Weight, int Priority, bool Enabled);
public sealed record ProxyPoolDto(Guid Id, string Name, string Strategy, int StickyTtlSeconds, bool Enabled, IReadOnlyList<ProxyPoolMemberDto> Members);
public sealed record ProxyPoolMemberRequest(Guid DeviceId, int Weight, int Priority);
public sealed record ProxySessionDto(Guid Id, Guid EndpointId, Guid DeviceId, string DestinationHost, int DestinationPort, DateTimeOffset StartedAt, DateTimeOffset EndedAt, long BytesUp, long BytesDown, string CloseReason);
public sealed record ProxyTrafficSummaryDto(int SessionCount, long BytesUp, long BytesDown, DateTimeOffset? MostRecentSessionAt);
public sealed record CreateProxyEndpointRequest(string Name, string BindAddress, int Port, string Protocol, Guid? DeviceId, Guid? PoolId);
public sealed record CreateProxyPoolRequest(string Name, string Strategy, int StickyTtlSeconds, IReadOnlyList<ProxyPoolMemberRequest> Members);
public sealed record UpdateProxyPoolRequest(string Strategy, int StickyTtlSeconds, bool Enabled);
public sealed record RotationResultDto(string CurrentIp, string PreviousIp, DateTimeOffset ChangedAt, string TriggeredBy, string Duration, string Status);
public sealed record RotateIpRequest(string? TriggeredBy);
public sealed record ProxyTestResultDto(Guid EndpointId, bool Reachable, int LatencyMs, string ResolvedWanIp, string Protocol, string Status);
public sealed record NetstatConnectionDto(string LocalAddress, int LocalPort, string RemoteAddress, int RemotePort, string State);
public sealed record NetworkInterfaceDto(string Id, string Name, string Description, string Status, string Type, long SpeedMbps, string IpAddresses, long BytesReceived, long BytesSent);
public sealed record PingRequest(string Host, int TimeoutMs = 3000);
public sealed record PingResultDto(string Host, string IpAddress, bool Success, long RoundTripTimeMs, int Ttl, string Status);
public sealed record DeviceTelemetryDto(string Serial, string? Carrier, string? Network, string? SignalDbm, string? Battery, string? Apn, string? Iccid, string? WanIp);
