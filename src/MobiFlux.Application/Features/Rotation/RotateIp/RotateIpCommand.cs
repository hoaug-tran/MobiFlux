using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Rotation.RotateIp;

public sealed record RotateIpCommand(string? TriggeredBy, Guid? DeviceId = null) : IRequest<RotationResultDto>;
