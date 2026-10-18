using MediatR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Rotation.GetRotationHistory;

public sealed class GetRotationHistoryHandler(IRotationHistory history) : IRequestHandler<GetRotationHistoryQuery, IReadOnlyList<RotationResultDto>>
{
    public Task<IReadOnlyList<RotationResultDto>> Handle(GetRotationHistoryQuery query, CancellationToken cancellationToken) => Task.FromResult(history.List());
}
