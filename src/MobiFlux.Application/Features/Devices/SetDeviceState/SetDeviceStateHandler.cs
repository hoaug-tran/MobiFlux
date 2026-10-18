using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.SetDeviceState;

public sealed class SetDeviceStateHandler(IDeviceRepository devices, TimeProvider clock, IProxyRepository proxies, IProxyRuntime runtime) : IRequestHandler<SetDeviceStateCommand, DeviceDto>
{
    public async Task<DeviceDto> Handle(SetDeviceStateCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.GetAsync(command.DeviceId, cancellationToken) ?? throw new KeyNotFoundException("Device was not found.");
        if (command.Enabled) device.Enable(clock.GetUtcNow()); else device.Disable(clock.GetUtcNow());
        await devices.SaveChangesAsync(cancellationToken);
        runtime.RefreshRoutes(await devices.ListAsync(cancellationToken), await proxies.ListPoolsAsync(cancellationToken));
        return new(device.Id, device.AdbSerial, device.Name, device.Enabled, device.DesiredState.ToString(), device.ActualState.ToString(), device.LastSeenAt);
    }
}
