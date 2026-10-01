using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Devices.GetDevices;

public sealed record GetDevicesQuery : IRequest<IReadOnlyList<DeviceDto>>;
