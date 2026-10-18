using Microsoft.AspNetCore.SignalR;
using MobiFlux.Application.Abstractions;
using MobiFlux.Application.Features.Devices.ReconcileDevices;
using MobiFlux.Infrastructure.Routing;
using MediatR;

namespace MobiFlux.Service.Services;

public sealed class RuntimeHub : Hub { }

public sealed class RuntimeReconciliationWorker(IServiceScopeFactory scopes, ILogger<RuntimeReconciliationWorker> logger, CachedRouteResolver routes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            do
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    await sender.Send(new ReconcileDevicesCommand(), stoppingToken);
                    var devices = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    var proxies = scope.ServiceProvider.GetRequiredService<IProxyRepository>();
                    routes.Refresh(await devices.ListAsync(stoppingToken), await proxies.ListPoolsAsync(stoppingToken));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception exception)
                {
                    routes.Refresh([], []);
                    logger.LogWarning(exception, "ADB reconciliation failed; proxy routes remain fail-closed.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
