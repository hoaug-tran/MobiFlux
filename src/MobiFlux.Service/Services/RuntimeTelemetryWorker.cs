using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Configuration;

namespace MobiFlux.Service.Services;

public sealed class RuntimeTelemetryWorker(IRuntimeTelemetry telemetry, IHubContext<RuntimeHub> hub, IOptions<TelemetryOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.SampleIntervalMilliseconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken)) await hub.Clients.All.SendAsync("RuntimeUpdated", telemetry.Sample(), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
