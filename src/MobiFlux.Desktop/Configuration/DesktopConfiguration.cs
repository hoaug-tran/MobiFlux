using Microsoft.Extensions.Configuration;

namespace MobiFlux.Desktop.Configuration;

public sealed class ServiceConnectionOptions
{
    public const string SectionName = "Service";
    public required Uri BaseAddress { get; init; }
}

public static class DesktopConfiguration
{
    private static string? _overrideLanguage;

    public static ServiceConnectionOptions Load()
    {
        var configuration = BuildConfiguration();
        var address = configuration[$"{ServiceConnectionOptions.SectionName}:BaseAddress"];
        if (!Uri.TryCreate(address, UriKind.Absolute, out var baseAddress) || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Service:BaseAddress must be an absolute HTTP(S) URI.");
        }
        return new() { BaseAddress = baseAddress };
    }

    public static string LoadLanguage()
    {
        if (!string.IsNullOrEmpty(_overrideLanguage))
        {
            return _overrideLanguage;
        }

        var configuration = BuildConfiguration();
        var language = configuration["Desktop:Language"];
        return string.IsNullOrWhiteSpace(language) ? "vi" : language.Trim().ToLowerInvariant();
    }

    public static void SaveLanguage(string language)
    {
        _overrideLanguage = language.Trim().ToLowerInvariant();
    }

    private static IConfigurationRoot BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables(prefix: "MOBIFLUX_")
            .Build();
}
