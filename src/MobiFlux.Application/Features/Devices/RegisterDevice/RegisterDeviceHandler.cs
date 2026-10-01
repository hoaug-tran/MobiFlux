using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.RegisterDevice;

public sealed class RegisterDeviceHandler(IDeviceRepository devices, TimeProvider clock) : IRequestHandler<RegisterDeviceCommand, DeviceDto>
{
    public async Task<DeviceDto> Handle(RegisterDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.GetByAdbSerialAsync(command.AdbSerial, cancellationToken);
        if (device is null)
        {
            device = new DeviceNode(command.AdbSerial, command.Name, clock.GetUtcNow());
            await devices.AddAsync(device, cancellationToken);
            await devices.SaveChangesAsync(cancellationToken);
        }
        return new(device.Id, device.AdbSerial, device.Name, device.Enabled, device.DesiredState.ToString(), device.ActualState.ToString(), device.LastSeenAt);
    }
}
