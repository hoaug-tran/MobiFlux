using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Rotation.GetRotationHistory;

public sealed record GetRotationHistoryQuery : IRequest<IReadOnlyList<RotationResultDto>>;
