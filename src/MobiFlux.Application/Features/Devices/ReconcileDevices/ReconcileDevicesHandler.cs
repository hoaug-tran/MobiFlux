using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;

namespace MobiFlux.Application.Features.Devices.ReconcileDevices;

public sealed class ReconcileDevicesHandler(IDeviceRepository devices, IAdbClient adb, TimeProvider clock) : IRequestHandler<ReconcileDevicesCommand, int>
{
    public async Task<int> Handle(ReconcileDevicesCommand command, CancellationToken cancellationToken)
    {
        var known = await devices.ListAsync(cancellationToken);
        var connected = await adb.ListDevicesAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var knownSerials = known.Select(item => item.AdbSerial).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var conn in connected)
        {
            if (!knownSerials.Contains(conn.Serial))
            {
                var name = !string.IsNullOrWhiteSpace(conn.Model) ? conn.Model : $"Device-{conn.Serial[..Math.Min(8, conn.Serial.Length)]}";
                var newDevice = new DeviceNode(conn.Serial, name, now);
                newDevice.SetIdentity(conn.Manufacturer, conn.Model, conn.AndroidVersion, now);
                newDevice.Enable(now);
                newDevice.Reconcile(conn.State == "device" ? DeviceActualState.ProxyReady : DeviceActualState.Discovered, now);
                await devices.AddAsync(newDevice, cancellationToken);
            }
        }

        foreach (var device in known)
        {
            var current = connected.FirstOrDefault(item => string.Equals(item.Serial, device.AdbSerial, StringComparison.OrdinalIgnoreCase));
            var state = current?.State switch
            {
                "device" => DeviceActualState.ProxyReady,
                "unauthorized" => DeviceActualState.Unauthorized,
                _ => DeviceActualState.Offline
            };
            device.Reconcile(state, now);
            if (current is not null)
            {
                device.SetIdentity(current.Manufacturer, current.Model, current.AndroidVersion, now);
            }
        }

        await devices.SaveChangesAsync(cancellationToken);
        var updated = await devices.ListAsync(cancellationToken);
        return updated.Count;
    }
}
