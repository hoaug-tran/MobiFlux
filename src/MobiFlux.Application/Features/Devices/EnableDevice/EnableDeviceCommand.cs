using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.EnableDevice;

public sealed record EnableDeviceCommand(Guid DeviceId) : IRequest<DeviceDto>;
