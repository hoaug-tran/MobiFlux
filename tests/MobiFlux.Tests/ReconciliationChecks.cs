using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Devices.ReconcileDevices;
using MobiFlux.Application.Features.Devices.GetDevices;
using MobiFlux.Application.Features.Rotation.RotateIp;
using MobiFlux.Domain.Devices;
using MobiFlux.Shared.Contracts;

internal static class ReconciliationChecks
{
    public static async Task RunAsync(List<string> failures)
    {
        var repository = new Devices();
        var adb = new Adb();
        var probe = new Probe();
        var handler = new ReconcileDevicesHandler(repository, adb, probe, TimeProvider.System);
        await handler.Handle(new ReconcileDevicesCommand(), default);
        var device = repository.Items.Single();
        if (device.ActualState != DeviceActualState.CellularUnavailable) failures.Add("ADB discovery must not imply cellular readiness");
        var listed = await new GetDevicesHandler(repository, adb).Handle(new GetDevicesQuery(), default);
        if (listed.Single().ActualState != "CellularUnavailable" || listed.Single().Battery is not null || listed.Single().WanIp is not null) failures.Add("device query must preserve verified state and unknown telemetry");
        probe.Available = true;
        await handler.Handle(new ReconcileDevicesCommand(), default);
        if (device.ActualState != DeviceActualState.ProxyReady) failures.Add("verified cellular agent becomes ready");
        probe.Error = true;
        await handler.Handle(new ReconcileDevicesCommand(), default);
        if (device.ActualState != DeviceActualState.AgentUnavailable) failures.Add("agent failure clears readiness");
        adb.State = "unauthorized";
        await handler.Handle(new ReconcileDevicesCommand(), default);
        if (device.ActualState != DeviceActualState.Unauthorized) failures.Add("unauthorized device state preserved");
        device.Disable(DateTimeOffset.UtcNow);
        adb.State = "device";
        await handler.Handle(new ReconcileDevicesCommand(), default);
        if (device.ActualState != DeviceActualState.Disabled) failures.Add("reconciliation must preserve disabled state");
        var history = new History();
        var rotation = new RotateIpHandler(adb, history, TimeProvider.System);
        adb.Override = [];
        try { await rotation.Handle(new RotateIpCommand(null), default); failures.Add("rotation must reject missing devices"); }
        catch (InvalidOperationException) { }
        adb.Override = [new("one", "device", null, null, null), new("two", "device", null, null, null)];
        try { await rotation.Handle(new RotateIpCommand(null), default); failures.Add("rotation must reject ambiguous devices"); }
        catch (InvalidOperationException) { }
        if (adb.RotationCalls != 0) failures.Add("rejected rotation must not toggle a device");
        adb.Override = null;
        var rotated = await rotation.Handle(new RotateIpCommand(null), default);
        if (rotated.CurrentIp.Length != 0 || rotated.PreviousIp.Length != 0 || adb.RotationCalls != 1 || history.Items.Count != 1)
            failures.Add("reconnect request must not invent an IP address");
    }

    private sealed class Probe : IAgentStatusProbe
    {
        public bool Available; public bool Error;
        public Task<bool> ProbeCellularAsync(string serial, Guid id, CancellationToken token) => Error ? throw new IOException("test agent unavailable") : Task.FromResult(Available);
    }
    private sealed class Adb : IAdbClient
    {
        public string State = "device";
        public IReadOnlyList<AdbDevice>? Override;
        public int RotationCalls;
        public Task<IReadOnlyList<AdbDevice>> ListDevicesAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<AdbDevice>>(Override ?? [new("test-serial", State, "Test phone", null, null)]);
        public Task<string> ExecuteShellAsync(string serial, string command, CancellationToken token) => throw new NotSupportedException();
        public Task<DeviceTelemetryDto> GetDeviceTelemetryAsync(string serial, CancellationToken token) => throw new NotSupportedException();
        public Task<bool> RotateDeviceIpAsync(string serial, CancellationToken token) { RotationCalls++; return Task.FromResult(true); }
    }
    private sealed class History : IRotationHistory
    {
        public readonly List<RotationResultDto> Items = [];
        public IReadOnlyList<RotationResultDto> List() => Items;
        public void Add(RotationResultDto result) => Items.Add(result);
    }
    private sealed class Devices : IDeviceRepository
    {
        public readonly List<DeviceNode> Items = [];
        public Task<DeviceNode?> GetAsync(Guid id, CancellationToken token) => Task.FromResult(Items.Find(item => item.Id == id));
        public Task<DeviceNode?> GetByAdbSerialAsync(string serial, CancellationToken token) => Task.FromResult(Items.Find(item => item.AdbSerial == serial));
        public Task<IReadOnlyList<DeviceNode>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<DeviceNode>>(Items.ToArray());
        public Task AddAsync(DeviceNode device, CancellationToken token) { Items.Add(device); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken token) => Task.CompletedTask;
    }
}
