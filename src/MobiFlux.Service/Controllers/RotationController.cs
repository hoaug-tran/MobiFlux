using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Common;
using MobiFlux.Shared.Contracts;
using MobiFlux.Shared.Routing;

namespace MobiFlux.Service.Controllers;

[Route(ApiRoutes.Rotation)]
public sealed class RotationController(IAdbClient adb) : ApiControllerBase
{
    private static readonly ConcurrentBag<RotationResultDto> _history = new();

    [HttpPost("rotate")]
    public async Task<ActionResult<ApiResponse<RotationResultDto>>> Rotate([FromBody] RotateIpRequest? request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var trigger = string.IsNullOrWhiteSpace(request?.TriggeredBy) ? "Thủ công (Manual)" : request.TriggeredBy;
        var prevIp = _history.TryPeek(out var last) ? last.CurrentIp : "127.0.0.1";

        var connectedDevices = await adb.ListDevicesAsync(cancellationToken);
        var targetDevice = connectedDevices.FirstOrDefault(x => x.State == "device");

        if (targetDevice is null)
        {
            sw.Stop();
            var fallbackItem = new RotationResultDto(
                "Chưa gắn thiết bị di động",
                prevIp,
                DateTimeOffset.UtcNow,
                trigger,
                $"{sw.ElapsedMilliseconds}ms",
                "Cần kết nối thiết bị qua USB Debugging"
            );
            _history.Add(fallbackItem);
            return Success(fallbackItem);
        }

        var success = await adb.RotateDeviceIpAsync(targetDevice.Serial, cancellationToken);
        sw.Stop();

        var telemetry = await adb.GetDeviceTelemetryAsync(targetDevice.Serial, cancellationToken);
        var newIp = telemetry.WanIp ?? (!string.IsNullOrEmpty(telemetry.Carrier) ? $"{telemetry.Carrier} IP" : targetDevice.Serial);

        var status = success ? "Thành công" : "Thất bại";
        var resultItem = new RotationResultDto(
            newIp,
            prevIp,
            DateTimeOffset.UtcNow,
            trigger,
            $"{sw.Elapsed.TotalSeconds:0.0}s",
            status
        );
        _history.Add(resultItem);

        return Success(resultItem);
    }

    [HttpGet("history")]
    public ActionResult<ApiResponse<IReadOnlyList<RotationResultDto>>> GetHistory()
    {
        return Success<IReadOnlyList<RotationResultDto>>(_history.OrderByDescending(h => h.ChangedAt).ToArray());
    }
}
