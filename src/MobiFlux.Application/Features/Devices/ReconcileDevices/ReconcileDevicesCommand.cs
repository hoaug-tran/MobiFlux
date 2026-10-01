using MediatR;

namespace MobiFlux.Application.Features.Devices.ReconcileDevices;

public sealed record ReconcileDevicesCommand : IRequest<int>;
