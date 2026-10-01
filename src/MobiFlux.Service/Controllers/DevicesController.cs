using MediatR;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Application.Features.Devices.GetDevices;
using MobiFlux.Application.Features.Devices.ReconcileDevices;
using MobiFlux.Application.Features.Devices.RegisterDevice;
using MobiFlux.Application.Features.Devices.SetDeviceState;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.Devices)]
public sealed class DevicesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceDto>>>> Get(CancellationToken cancellationToken) => Success(await sender.Send(new GetDevicesQuery(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeviceDto>>> Register(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var device = await sender.Send(new RegisterDeviceCommand(request.AdbSerial, request.Name), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = device.Id }, ApiResponse<DeviceDto>.Ok(device, HttpContext.TraceIdentifier));
    }

    [HttpPost("~/" + ApiRoutes.DeviceDiscoveryRuns)]
    public async Task<ActionResult<ApiResponse<int>>> Discover(CancellationToken cancellationToken) => Success(await sender.Send(new ReconcileDevicesCommand(), cancellationToken));

    [HttpPatch("{deviceId:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceDto>>> SetState(Guid deviceId, DeviceStateRequest request, CancellationToken cancellationToken) => Success(await sender.Send(new SetDeviceStateCommand(deviceId, request.Enabled), cancellationToken));
}
