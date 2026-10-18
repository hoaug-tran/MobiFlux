using System.IO;
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
    private static readonly string PreferencesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MobiFlux", "desktop.json");
    public static DesktopPreferences LoadPreferences()
    {
        try { return System.Text.Json.JsonSerializer.Deserialize<DesktopPreferences>(File.ReadAllText(PreferencesPath)) ?? new(null, null); }
        catch (IOException) { return new(null, null); }
        catch (System.Text.Json.JsonException) { return new(null, null); }
    }
    public static void SavePreferences(DesktopPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
        var temporary = PreferencesPath + ".tmp";
        File.WriteAllText(temporary, System.Text.Json.JsonSerializer.Serialize(preferences));
        File.Move(temporary, PreferencesPath, overwrite: true);
    }

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

public sealed record DesktopPreferences(string? ApiAddress, string? AdvertisedAddress, bool AutoRefresh = true, string? ServiceExecutable = null, string Theme = "Light");
