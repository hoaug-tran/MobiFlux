using MobiFlux.Domain.Devices;

namespace MobiFlux.Application.Proxies;

public static class DeviceEligibility
{
    public static bool CanProxy(DeviceNode device, DeviceCapabilities? capabilities) =>
        device.Enabled && device.ActualState == DeviceActualState.ProxyReady && capabilities is { CanProxyTcp: true, CanResolveDnsOnCellular: true };
}
