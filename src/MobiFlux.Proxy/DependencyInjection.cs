using Microsoft.Extensions.DependencyInjection;

namespace MobiFlux.Proxy;

public static class DependencyInjection
{
    public static IServiceCollection AddProxy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ProxyGateway>();
        return services;
    }
}
