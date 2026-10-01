using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Shared.Configuration;

namespace MobiFlux.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var storage = configuration.GetRequiredSection(StorageOptions.SectionName).Get<StorageOptions>() ?? throw new InvalidOperationException("Storage configuration is required.");
        var adb = configuration.GetRequiredSection(AdbOptions.SectionName).Get<AdbOptions>() ?? throw new InvalidOperationException("ADB configuration is required.");
        var proxy = configuration.GetRequiredSection(ProxyOptions.SectionName).Get<ProxyOptions>() ?? throw new InvalidOperationException("Proxy configuration is required.");
        var routing = configuration.GetRequiredSection(RoutingOptions.SectionName).Get<RoutingOptions>() ?? throw new InvalidOperationException("Routing configuration is required.");
        var sessions = configuration.GetRequiredSection(SessionPersistenceOptions.SectionName).Get<SessionPersistenceOptions>() ?? throw new InvalidOperationException("Session configuration is required.");
        var api = configuration.GetRequiredSection(ApiOptions.SectionName).Get<ApiOptions>() ?? throw new InvalidOperationException("API configuration is required.");
        if (!Enum.TryParse<DnsMode>(proxy.DefaultDnsMode, true, out var dnsMode)) throw new InvalidOperationException("Proxy default DNS mode is invalid.");
        services.AddSingleton<IProxyDefaults>(new ProxyDefaults(proxy.DefaultMaxConnections, dnsMode));
        services.AddSingleton<IApiRequestLimits>(new ApiRequestLimits(api.MaximumPageSize));
        return services.AddMobiFluxInfrastructure(storage.DatabasePath, adb.ExecutablePath, adb.AgentForwardPortStart, routing.MaxStickyEntries, sessions.BufferCapacity);
    }

    private sealed class ProxyDefaults(int maxConnections, DnsMode dnsMode) : IProxyDefaults
    {
        public int MaxConnections { get; } = maxConnections;
        public DnsMode DnsMode { get; } = dnsMode;
    }

    private sealed class ApiRequestLimits(int maximumPageSize) : IApiRequestLimits
    {
        public int MaximumPageSize { get; } = maximumPageSize;
    }
}
