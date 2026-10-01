using System.ComponentModel.DataAnnotations;

namespace MobiFlux.Shared.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    [Required, MinLength(1)] public string DatabasePath { get; init; } = null!;
}

public sealed class AdbOptions
{
    public const string SectionName = "Adb";
    [Required, MinLength(1)] public string ExecutablePath { get; init; } = null!;
    [Range(1024, 65534)] public int AgentForwardPortStart { get; init; }
}

public sealed class ProxyOptions
{
    public const string SectionName = "Proxy";
    [Range(1, 65_535)] public int DefaultMaxConnections { get; init; }
    [Required, MinLength(1)] public string DefaultDnsMode { get; init; } = null!;
    [Range(1, 3_600)] public int ConnectTimeoutSeconds { get; init; }
    [Range(1, 3_600)] public int IdleTimeoutSeconds { get; init; }
}

public sealed class RoutingOptions
{
    public const string SectionName = "Routing";
    [Range(1, 1_000_000)] public int MaxStickyEntries { get; init; }
}

public sealed class SessionPersistenceOptions
{
    public const string SectionName = "Sessions";
    [Range(1, 1_000_000)] public int BufferCapacity { get; init; }
    [Range(1, 10_000)] public int PersistenceBatchSize { get; init; }
    [Range(1, 3_600)] public int PersistenceIntervalSeconds { get; init; }
    [Range(0, 100)] public int PersistenceRetryCount { get; init; }
    [Range(1, 3_600)] public int PersistenceRetryDelaySeconds { get; init; }
}

public sealed class ApiOptions
{
    public const string SectionName = "Api";
    [Range(1, 10_000)] public int MaximumPageSize { get; init; }
}
