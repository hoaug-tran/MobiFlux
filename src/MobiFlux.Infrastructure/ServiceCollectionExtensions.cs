using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MobiFlux.Application.Abstractions;
using MobiFlux.Infrastructure.Adb;
using MobiFlux.Infrastructure.AndroidAgent;
using MobiFlux.Infrastructure.Persistence;
using MobiFlux.Infrastructure.Routing;
using MobiFlux.Infrastructure.Runtime;
using MobiFlux.Infrastructure.Telemetry;
using MobiFlux.Proxy;

namespace MobiFlux.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMobiFluxInfrastructure(this IServiceCollection services, string databasePath, string adbExecutable, int agentForwardPortStart, int maxStickyEntries, int sessionBufferCapacity)
    {
        var directory = Path.GetDirectoryName(databasePath); if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        services.AddDbContext<MobiFluxDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<EfMobiFluxRepository>();
        services.AddScoped<IDeviceRepository>(provider => provider.GetRequiredService<EfMobiFluxRepository>());
        services.AddScoped<IProxyRepository>(provider => provider.GetRequiredService<EfMobiFluxRepository>());
        services.AddScoped<IProxySessionRepository>(provider => provider.GetRequiredService<EfMobiFluxRepository>());
        services.AddSingleton<IAdbClient>(_ => new AdbClient(adbExecutable));
        services.AddSingleton(new AgentForwardRegistry(agentForwardPortStart)); services.AddSingleton<IDeviceTunnelFactory, AgentTunnelFactory>();
        services.AddSingleton<AgentStatusProbe>(); services.AddSingleton<IAdbForwarder>(provider => (IAdbForwarder)provider.GetRequiredService<IAdbClient>());
        services.AddSingleton<CachedRouteResolver>(provider => new CachedRouteResolver(provider.GetRequiredService<TimeProvider>(), maxStickyEntries));
        services.AddSingleton<IRouteResolver>(provider => provider.GetRequiredService<CachedRouteResolver>());
        services.AddSingleton(new ProxySessionBuffer(sessionBufferCapacity));
        services.AddSingleton<IProxySessionQueue>(provider => provider.GetRequiredService<ProxySessionBuffer>());
        services.AddSingleton<BufferedProxySessionObserver>();
        services.AddSingleton<IProxySessionObserver>(provider => new ConnectionTrackingProxySessionObserver(provider.GetRequiredService<BufferedProxySessionObserver>(), provider.GetRequiredService<CachedRouteResolver>()));
        services.AddSingleton<IProxyRuntime, ProxyRuntime>();
        return services;
    }
}
