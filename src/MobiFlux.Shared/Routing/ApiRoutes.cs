namespace MobiFlux.Shared.Routing;

public static class ApiRoutes
{
    public const string Root = "api/v1/mobiflux";
    public const string Devices = Root + "/devices";
    public const string DeviceDiscoveryRuns = Root + "/device-discovery-runs";
    public const string Proxies = Root + "/proxies";
    public const string ProxyRuntime = Root + "/proxy-runtime";
    public const string Pools = Root + "/proxy-pools";
    public const string ProxySessions = Root + "/proxy-sessions";
    public const string ProxyTraffic = Root + "/proxy-traffic";
    public const string Rotation = Root + "/rotation";
    public const string Networking = Root + "/networking";
    public const string Settings = Root + "/settings";
}
