using MobiFlux.Domain.Devices;

namespace MobiFlux.Domain.Proxies;

public sealed class ProxyEndpoint
{
    private ProxyEndpoint() { }
    public ProxyEndpoint(string name, string bindAddress, int port, ProxyProtocol protocol, Guid? deviceId, Guid? poolId, DnsMode dnsMode, int maxConnections)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Endpoint name is required.", nameof(name));
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if ((deviceId is null) == (poolId is null)) throw new ArgumentException("An endpoint must target exactly one device or pool.");
        if (maxConnections < 1) throw new ArgumentOutOfRangeException(nameof(maxConnections));
        Id = Guid.NewGuid(); Name = name.Trim(); BindAddress = bindAddress; Port = port; Protocol = protocol; DeviceId = deviceId; PoolId = poolId; DnsMode = dnsMode; MaxConnections = maxConnections;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string BindAddress { get; private set; } = null!;
    public int Port { get; private set; }
    public ProxyProtocol Protocol { get; private set; }
    public Guid? DeviceId { get; private set; }
    public Guid? PoolId { get; private set; }
    public bool Enabled { get; private set; } = true;
    public DnsMode DnsMode { get; private set; }
    public int MaxConnections { get; private set; }
    public void SetEnabled(bool enabled) => Enabled = enabled;
}

public sealed class ProxyPool
{
    private ProxyPool() { }
    public ProxyPool(string name, RoutingStrategy strategy, int stickyTtlSeconds)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Pool name is required.", nameof(name));
        if (stickyTtlSeconds is < 0 or > 86_400) throw new ArgumentOutOfRangeException(nameof(stickyTtlSeconds));
        Id = Guid.NewGuid(); Name = name.Trim(); RoutingStrategy = strategy; StickyTtlSeconds = stickyTtlSeconds;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool Enabled { get; private set; } = true;
    public RoutingStrategy RoutingStrategy { get; private set; }
    public int StickyTtlSeconds { get; private set; }
    public ICollection<ProxyPoolMember> Members { get; private set; } = new List<ProxyPoolMember>();
    public void AddMember(Guid deviceId, int weight, int priority)
    {
        if (Members.Any(member => member.DeviceId == deviceId)) throw new ArgumentException("A device can belong to a pool only once.", nameof(deviceId));
        Members.Add(new ProxyPoolMember(Id, deviceId, weight, priority));
    }
    public void Update(RoutingStrategy strategy, int stickyTtlSeconds, bool enabled)
    {
        if (stickyTtlSeconds is < 0 or > 86_400) throw new ArgumentOutOfRangeException(nameof(stickyTtlSeconds));
        RoutingStrategy = strategy;
        StickyTtlSeconds = stickyTtlSeconds;
        Enabled = enabled;
    }
}

public sealed class ProxyPoolMember
{
    private ProxyPoolMember() { }
    public ProxyPoolMember(Guid poolId, Guid deviceId, int weight, int priority)
    {
        if (weight < 1) throw new ArgumentOutOfRangeException(nameof(weight));
        PoolId = poolId; DeviceId = deviceId; Weight = weight; Priority = priority;
    }
    public Guid PoolId { get; private set; }
    public Guid DeviceId { get; private set; }
    public bool Enabled { get; private set; } = true;
    public int Weight { get; private set; }
    public int Priority { get; private set; }
}
