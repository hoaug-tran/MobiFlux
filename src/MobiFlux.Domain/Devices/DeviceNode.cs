namespace MobiFlux.Domain.Devices;

public enum DeviceDesiredState { Disabled, Enabled }
public enum DeviceActualState { Discovered, Unauthorized, Offline, AgentUnavailable, CellularUnavailable, ProxyReady, Degraded, Disabled }
public enum ProxyProtocol { Socks5, HttpConnect }
public enum RoutingStrategy { RoundRobin, LeastConnections, WeightedRoundRobin, Random }
public enum DnsMode { RemoteOnly, RemotePreferred }

public sealed class DeviceNode
{
    private DeviceNode() { }

    public DeviceNode(string adbSerial, string name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(adbSerial)) throw new ArgumentException("ADB serial is required.", nameof(adbSerial));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Device name is required.", nameof(name));
        Id = Guid.NewGuid(); AdbSerial = adbSerial.Trim(); Name = name.Trim();
        DesiredState = DeviceDesiredState.Disabled; ActualState = DeviceActualState.Discovered;
        CreatedAt = UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public string AdbSerial { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public string? AndroidVersion { get; private set; }
    public string? AgentVersion { get; private set; }
    public bool Enabled => DesiredState == DeviceDesiredState.Enabled;
    public DeviceDesiredState DesiredState { get; private set; }
    public DeviceActualState ActualState { get; private set; }
    public Guid? GroupId { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Enable(DateTimeOffset now) { DesiredState = DeviceDesiredState.Enabled; Touch(now); }
    public void Disable(DateTimeOffset now) { DesiredState = DeviceDesiredState.Disabled; ActualState = DeviceActualState.Disabled; Touch(now); }
    public void Reconcile(DeviceActualState actualState, DateTimeOffset now)
    {
        ActualState = DesiredState == DeviceDesiredState.Disabled ? DeviceActualState.Disabled : actualState;
        LastSeenAt = now; Touch(now);
    }
    public void SetIdentity(string? manufacturer, string? model, string? androidVersion, DateTimeOffset now)
    { Manufacturer = manufacturer; Model = model; AndroidVersion = androidVersion; Touch(now); }
    private void Touch(DateTimeOffset now) => UpdatedAt = now;
}

public sealed class DeviceCapabilities
{
    public Guid DeviceId { get; set; }
    public bool CanProxyTcp { get; set; }
    public bool CanProxyUdp { get; set; }
    public bool CanResolveDnsOnCellular { get; set; }
    public bool CanReadCarrier { get; set; }
    public bool CanReadSignal { get; set; }
    public bool CanReadBattery { get; set; }
    public bool CanRestartAgent { get; set; }
    public bool CanToggleMobileData { get; set; }
    public bool CanToggleAirplaneMode { get; set; }
    public bool SupportsIpv4 { get; set; }
    public bool SupportsIpv6 { get; set; }
}
