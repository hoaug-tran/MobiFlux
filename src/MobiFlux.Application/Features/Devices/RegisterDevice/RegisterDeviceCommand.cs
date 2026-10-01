using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.RegisterDevice;

public sealed record RegisterDeviceCommand(string AdbSerial, string Name) : IRequest<DeviceDto>;
