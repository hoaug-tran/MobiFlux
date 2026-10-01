using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.SetDeviceState;

public sealed record SetDeviceStateCommand(Guid DeviceId, bool Enabled) : IRequest<DeviceDto>;
