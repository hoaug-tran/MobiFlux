using MobiFlux.Domain.Devices;
using System.Net;
using System.Security.Cryptography;
using System.Text;

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
    public string? Username { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? PasswordSalt { get; private set; }
    public string AllowedClients { get; private set; } = string.Empty;
    public bool RequiresAuthentication => !string.IsNullOrEmpty(Username);
    public void ConfigureAccess(string? username, string? password, string? allowedClients)
    {
        if (!IPAddress.TryParse(BindAddress, out var bind)) throw new ArgumentException("Invalid bind address.");
        var clients = (allowedClients ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (clients.Any(client => !IPAddress.TryParse(client, out _))) throw new ArgumentException("Allowed clients must be comma-separated IP addresses.");
        if (!IPAddress.IsLoopback(bind) && (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password) || clients.Length == 0))
            throw new ArgumentException("LAN listeners require username, password and allowed client IP addresses.");
        if (string.IsNullOrWhiteSpace(username) != string.IsNullOrEmpty(password)) throw new ArgumentException("Provide both username and password.");
        if (username is not null && (username.Contains(':') || Encoding.UTF8.GetByteCount(username) > 255)) throw new ArgumentException("Invalid proxy username.");
        if (password is not null && (password.Length < 12 || Encoding.UTF8.GetByteCount(password) > 255)) throw new ArgumentException("Proxy password must have at least 12 characters and at most 255 UTF-8 bytes.");
        Username = string.IsNullOrWhiteSpace(username) ? null : username;
        PasswordSalt = Username is null ? null : Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        PasswordHash = Username is null ? null : Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password!, Convert.FromBase64String(PasswordSalt!), 100_000, HashAlgorithmName.SHA256, 32));
        AllowedClients = string.Join(',', clients.Select(client => IPAddress.Parse(client).MapToIPv6().ToString()));
    }
    public bool Authenticate(string username, string password)
    {
        if (!RequiresAuthentication || PasswordSalt is null || PasswordHash is null) return false;
        var candidate = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(PasswordSalt), 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(candidate, Convert.FromBase64String(PasswordHash)) && string.Equals(username, Username, StringComparison.Ordinal);
    }
    public bool AllowsClient(IPAddress address) => string.IsNullOrEmpty(AllowedClients) || AllowedClients.Split(',').Contains(address.MapToIPv6().ToString(), StringComparer.Ordinal);
    public void Update(string name, int maxConnections, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 128) throw new ArgumentException("Invalid endpoint name.");
        if (maxConnections is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(maxConnections));
        Name = name.Trim(); MaxConnections = maxConnections; Enabled = enabled;
    }
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
    public void ReplaceMembers(IEnumerable<ProxyPoolMember> members)
    {
        var replacement = members.ToArray();
        if (replacement.Length == 0 || replacement.Select(member => member.DeviceId).Distinct().Count() != replacement.Length) throw new ArgumentException("Pool requires unique device members.");
        Members.Clear();
        foreach (var member in replacement) Members.Add(member);
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
    public void Update(int weight, int priority)
    {
        if (weight is < 1 or > 10000 || priority < 0) throw new ArgumentOutOfRangeException(nameof(weight));
        Weight = weight; Priority = priority;
    }
}
