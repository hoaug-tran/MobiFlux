using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.GetDevices;

public sealed class GetDevicesHandler(IDeviceRepository devices, IAdbClient adb) : IRequestHandler<GetDevicesQuery, IReadOnlyList<DeviceDto>>
{
    public async Task<IReadOnlyList<DeviceDto>> Handle(GetDevicesQuery query, CancellationToken cancellationToken)
    {
        var result = new List<DeviceDto>();
        foreach (var device in await devices.ListAsync(cancellationToken))
        {
            DeviceTelemetryDto? telemetry = null;
            if (device.Enabled && device.ActualState == DeviceActualState.ProxyReady)
            {
                try { telemetry = await adb.GetDeviceTelemetryAsync(device.AdbSerial, cancellationToken); }
                catch (Exception exception) when (exception is IOException or OperationCanceledException) { cancellationToken.ThrowIfCancellationRequested(); }
            }
            result.Add(new(device.Id, device.AdbSerial, device.Name, device.Enabled, device.DesiredState.ToString(), device.ActualState.ToString(), device.LastSeenAt,
                telemetry?.Carrier, telemetry?.Network, telemetry?.SignalDbm, telemetry?.Battery, null, telemetry?.Apn, telemetry?.Iccid, telemetry?.WanIp));
        }
        return result;
    }
}
