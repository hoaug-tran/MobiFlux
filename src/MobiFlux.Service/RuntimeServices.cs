using Microsoft.AspNetCore.SignalR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Devices.ReconcileDevices;
using MobiFlux.Domain.Devices;
using MobiFlux.Infrastructure.AndroidAgent;
using MediatR;

namespace MobiFlux.Service.Services;

public sealed class RuntimeHub : Hub { }

public sealed class RuntimeReconciliationWorker(IServiceScopeFactory scopes, ILogger<RuntimeReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ReconcileDevicesCommand(), stoppingToken);
                var devices = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                var probe = scope.ServiceProvider.GetRequiredService<AgentStatusProbe>();
                var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
                foreach (var device in await devices.ListAsync(stoppingToken))
                {
                    if (!device.Enabled || device.ActualState is DeviceActualState.Offline or DeviceActualState.Unauthorized or DeviceActualState.Disabled) continue;
                    try { device.Reconcile(await probe.ProbeCellularAsync(device.AdbSerial, device.Id, stoppingToken) ? DeviceActualState.ProxyReady : DeviceActualState.CellularUnavailable, clock.GetUtcNow()); }
                    catch (Exception exception) when (exception is IOException or HttpRequestException or TaskCanceledException) { device.Reconcile(DeviceActualState.AgentUnavailable, clock.GetUtcNow()); }
                }
                await devices.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogWarning(exception, "ADB reconciliation failed; proxy routes remain fail-closed."); }
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
