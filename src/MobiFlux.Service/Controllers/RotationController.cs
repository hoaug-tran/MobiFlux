using MediatR;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Application.Features.Rotation.GetRotationHistory;
using MobiFlux.Application.Features.Rotation.RotateIp;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.Rotation)]
public sealed class RotationController(ISender sender) : ApiControllerBase
{
    [HttpPost("rotate")]
    public async Task<ActionResult<ApiResponse<RotationResultDto>>> Rotate([FromBody] RotateIpRequest? request, CancellationToken cancellationToken) =>
        Success(await sender.Send(new RotateIpCommand(request?.TriggeredBy, request?.DeviceId), cancellationToken));

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RotationResultDto>>>> GetHistory(CancellationToken cancellationToken) =>
        Success(await sender.Send(new GetRotationHistoryQuery(), cancellationToken));
}
