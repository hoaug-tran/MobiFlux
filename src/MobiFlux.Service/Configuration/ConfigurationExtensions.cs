namespace MobiFlux.Service.Configuration;

public static class ConfigurationExtensions
{
    public static ConfigurationManager AddMobiFluxEnvironment(this ConfigurationManager configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment() && File.Exists(Path.Combine(environment.ContentRootPath, ".env"))) DotNetEnv.Env.Load();
        configuration.AddEnvironmentVariables(prefix: "MOBIFLUX_");
        return configuration;
    }
}
