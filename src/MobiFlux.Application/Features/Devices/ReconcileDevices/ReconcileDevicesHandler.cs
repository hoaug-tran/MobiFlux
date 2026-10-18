using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;

namespace MobiFlux.Application.Features.Devices.ReconcileDevices;

public sealed class ReconcileDevicesHandler(IDeviceRepository devices, IAdbClient adb, IAgentStatusProbe probe, TimeProvider clock) : IRequestHandler<ReconcileDevicesCommand, int>
{
    public async Task<int> Handle(ReconcileDevicesCommand command, CancellationToken cancellationToken)
    {
        var known = await devices.ListAsync(cancellationToken);
        var allDevices = known.ToList();
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
                newDevice.Reconcile(DeviceActualState.Discovered, now);
                await devices.AddAsync(newDevice, cancellationToken);
                allDevices.Add(newDevice);
                knownSerials.Add(conn.Serial);
            }
        }

        foreach (var device in allDevices)
        {
            var current = connected.FirstOrDefault(item => string.Equals(item.Serial, device.AdbSerial, StringComparison.OrdinalIgnoreCase));
            var state = current?.State switch
            {
                "device" => DeviceActualState.AgentUnavailable,
                "unauthorized" => DeviceActualState.Unauthorized,
                _ => DeviceActualState.Offline
            };
            if (device.Enabled && current?.State == "device")
            {
                try { state = await probe.ProbeCellularAsync(device.AdbSerial, device.Id, cancellationToken) ? DeviceActualState.ProxyReady : DeviceActualState.CellularUnavailable; }
                catch (Exception exception) when (exception is IOException or HttpRequestException or System.Text.Json.JsonException or OperationCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    state = DeviceActualState.AgentUnavailable;
                }
            }
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
