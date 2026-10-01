using MobiFlux.Service.Configuration;
using MobiFlux.Service.Middleware;
using MobiFlux.Service.Services;
using MobiFlux.Shared.Configuration;

namespace MobiFlux.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AdbOptions>().BindConfiguration(AdbOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ProxyOptions>().BindConfiguration(ProxyOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<RoutingOptions>().BindConfiguration(RoutingOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SessionPersistenceOptions>().BindConfiguration(SessionPersistenceOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ApiOptions>().BindConfiguration(ApiOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<RuntimeReconciliationWorker>();
        services.AddHostedService<ProxySessionPersistenceWorker>();
        services.AddControllers(); services.AddSignalR(); services.AddHealthChecks(); services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
