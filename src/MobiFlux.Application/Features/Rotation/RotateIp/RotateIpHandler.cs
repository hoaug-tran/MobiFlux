using System.Diagnostics;
using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Rotation.RotateIp;

public sealed class RotateIpHandler(IAdbClient adb, IRotationHistory history, TimeProvider clock, IDeviceRepository? devices = null, IDeviceEgressProbe? egress = null, IAgentStatusProbe? probe = null) : IRequestHandler<RotateIpCommand, RotationResultDto>
{
    public async Task<RotationResultDto> Handle(RotateIpCommand command, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var connected = await adb.ListDevicesAsync(cancellationToken);
        var candidates = connected.Where(device => device.State == "device").ToArray();
        var deviceId = command.DeviceId;
        string serial;
        if (deviceId is { } selected)
        {
            var device = devices is null ? null : await devices.GetAsync(selected, cancellationToken);
            if (device is null) throw new KeyNotFoundException("Selected Android device was not found.");
            if (!device.Enabled) throw new InvalidOperationException("Enable the selected device before reconnection.");
            if (!candidates.Any(candidate => candidate.Serial == device.AdbSerial)) throw new InvalidOperationException("Selected Android device is not USB-authorized and connected.");
            serial = device.AdbSerial;
        }
        else
        {
            if (candidates.Length != 1) throw new InvalidOperationException("Select exactly one connected Android device for reconnection.");
            serial = candidates[0].Serial;
            deviceId = devices is null ? null : (await devices.GetByAdbSerialAsync(serial, cancellationToken))?.Id;
        }
        string before = string.Empty, after = string.Empty;
        if (deviceId is { } id && egress is not null && probe is not null)
        {
            if (!await probe.ProbeCellularAsync(serial, id, cancellationToken)) throw new InvalidOperationException("Cellular agent is not ready; cannot verify egress before reconnection.");
            before = await egress.GetIpAsync(id, cancellationToken);
        }
        var requested = await adb.RotateDeviceIpAsync(serial, cancellationToken);
        var status = requested ? "Đã yêu cầu kết nối lại; chưa xác minh IP thay đổi" : "Không thể yêu cầu kết nối lại";
        if (requested && deviceId is { } target && egress is not null && probe is not null)
        {
            var deadline = clock.GetUtcNow().AddSeconds(35);
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                try
                {
                    if (!await probe.ProbeCellularAsync(serial, target, cancellationToken)) continue;
                    after = await egress.GetIpAsync(target, cancellationToken);
                    break;
                }
                catch (Exception exception) when (exception is IOException or HttpRequestException or OperationCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (clock.GetUtcNow() < deadline);
            status = after.Length == 0 ? "Đã kết nối lại nhưng chưa đo được IP cellular sau thao tác" : before == after ? "Cellular đã phục hồi; nhà mạng giữ nguyên IP" : "Đã xác minh IP cellular thay đổi";
        }
        var result = new RotationResultDto(after, before, clock.GetUtcNow(),
            string.IsNullOrWhiteSpace(command.TriggeredBy) ? "Thủ công (Manual)" : command.TriggeredBy.Trim(),
            $"{stopwatch.Elapsed.TotalSeconds:0.0}s", status, deviceId, serial);
        history.Add(result);
        return result;
    }
}
