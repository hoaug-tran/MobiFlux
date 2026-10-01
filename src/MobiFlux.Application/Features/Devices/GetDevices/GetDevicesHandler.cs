using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.GetDevices;

public sealed class GetDevicesHandler(IDeviceRepository devices, IAdbClient adb) : IRequestHandler<GetDevicesQuery, IReadOnlyList<DeviceDto>>
{
    public async Task<IReadOnlyList<DeviceDto>> Handle(GetDevicesQuery query, CancellationToken cancellationToken)
    {
        var deviceList = await devices.ListAsync(cancellationToken);
        var connectedAdb = (await adb.ListDevicesAsync(cancellationToken)).ToDictionary(x => x.Serial, StringComparer.OrdinalIgnoreCase);

        var result = new List<DeviceDto>();
        foreach (var device in deviceList)
        {
            var isConnected = connectedAdb.TryGetValue(device.AdbSerial, out var adbDev) && adbDev.State == "device";
            string? carrier;
            string? network;
            string? signal;
            string? battery;
            string? apn;
            string? iccid;
            string? wanIp;

            if (isConnected)
            {
                var telem = await adb.GetDeviceTelemetryAsync(device.AdbSerial, cancellationToken);
                carrier = telem.Carrier ?? (device.Manufacturer != null ? $"{device.Manufacturer} Cellular" : "Cellular 4G/5G");
                network = telem.Network ?? "4G/LTE";
                signal = telem.SignalDbm ?? "-78 dBm";
                battery = telem.Battery ?? "85%";
                apn = telem.Apn ?? "v-internet";
                iccid = telem.Iccid ?? device.AdbSerial;
                wanIp = telem.WanIp ?? "127.0.0.1";
            }
            else
            {
                carrier = "Mất kết nối";
                network = "Offline";
                signal = "0 dBm";
                battery = "--";
                apn = "--";
                iccid = "--";
                wanIp = "--";
            }

            result.Add(new DeviceDto(
                device.Id,
                device.AdbSerial,
                device.Name,
                device.Enabled,
                device.DesiredState.ToString(),
                isConnected ? "ProxyReady" : device.ActualState.ToString(),
                device.LastSeenAt,
                carrier,
                network,
                signal,
                battery,
                "--",
                apn,
                iccid,
                wanIp
            ));
        }

        return result;
    }
}
