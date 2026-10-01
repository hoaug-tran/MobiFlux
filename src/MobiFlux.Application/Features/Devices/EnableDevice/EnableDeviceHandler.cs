using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.EnableDevice;

public sealed class EnableDeviceHandler(IDeviceRepository devices, TimeProvider clock) : IRequestHandler<EnableDeviceCommand, DeviceDto>
{
    public async Task<DeviceDto> Handle(EnableDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.GetAsync(command.DeviceId, cancellationToken) ?? throw new KeyNotFoundException("Device was not found.");
        device.Enable(clock.GetUtcNow()); await devices.SaveChangesAsync(cancellationToken);
        return new(device.Id, device.AdbSerial, device.Name, device.Enabled, device.DesiredState.ToString(), device.ActualState.ToString(), device.LastSeenAt);
    }
}
