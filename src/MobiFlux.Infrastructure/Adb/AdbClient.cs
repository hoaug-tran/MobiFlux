using System.Diagnostics;
using System.Text.RegularExpressions;
using MobiFlux.Application.Abstractions;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Infrastructure.Adb;

public sealed class AdbClient(string executablePath) : IAdbClient, IAdbForwarder
{
    public async Task<IReadOnlyList<AdbDevice>> ListDevicesAsync(CancellationToken cancellationToken)
    {
        var output = await ExecuteAsync("devices -l", cancellationToken);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(line => !line.StartsWith("List of devices attached", StringComparison.Ordinal)).Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseDevice).Where(x => x is not null).Cast<AdbDevice>().ToArray();
    }

    public Task ForwardAsync(string serial, int localPort, int devicePort, CancellationToken token) =>
        ExecuteAsync($"-s {Quote(serial)} forward tcp:{localPort} tcp:{devicePort}", token);

    public Task<string> ExecuteShellAsync(string serial, string command, CancellationToken cancellationToken) =>
        ExecuteAsync($"-s {Quote(serial)} shell {command}", cancellationToken);

    public async Task<DeviceTelemetryDto> GetDeviceTelemetryAsync(string serial, CancellationToken cancellationToken)
    {
        string? carrier = null;
        string? network = null;
        string? signalDbm = null;
        string? battery = null;
        string? apn = null;
        string? iccid = null;
        string? wanIp = null;

        try
        {
            var carrierOutput = await ExecuteShellAsync(serial, "getprop gsm.sim.operator.alpha", cancellationToken);
            if (string.IsNullOrWhiteSpace(carrierOutput))
            {
                carrierOutput = await ExecuteShellAsync(serial, "getprop gsm.operator.alpha", cancellationToken);
            }
            if (!string.IsNullOrWhiteSpace(carrierOutput))
            {
                carrier = carrierOutput.Trim();
            }
        }
        catch { }

        try
        {
            var netOutput = await ExecuteShellAsync(serial, "getprop gsm.network.type", cancellationToken);
            if (!string.IsNullOrWhiteSpace(netOutput))
            {
                network = netOutput.Trim();
            }
        }
        catch { }

        try
        {
            var batteryOutput = await ExecuteShellAsync(serial, "dumpsys battery", cancellationToken);
            var match = Regex.Match(batteryOutput, @"level:\s*(\d+)");
            if (match.Success)
            {
                battery = $"{match.Groups[1].Value}%";
            }
        }
        catch { }

        try
        {
            var telOutput = await ExecuteShellAsync(serial, "dumpsys telephony.registry", cancellationToken);
            var dbmMatch = Regex.Match(telOutput, @"(?:mRssi|rsrp|dbm)=?(-?\d+)", RegexOptions.IgnoreCase);
            if (dbmMatch.Success && int.TryParse(dbmMatch.Groups[1].Value, out var val) && val is < 0 and > -150)
            {
                signalDbm = $"{val} dBm";
            }
            else
            {
                signalDbm = "-80 dBm";
            }
        }
        catch { }

        try
        {
            var simState = await ExecuteShellAsync(serial, "getprop gsm.sim.state", cancellationToken);
            if (!string.IsNullOrWhiteSpace(simState))
            {
                iccid = simState.Trim();
            }
        }
        catch { }

        try
        {
            var ipOutput = await ExecuteShellAsync(serial, "ip route get 8.8.8.8", cancellationToken);
            var ipMatch = Regex.Match(ipOutput, @"src\s+(\d+\.\d+\.\d+\.\d+)");
            if (ipMatch.Success)
            {
                wanIp = ipMatch.Groups[1].Value;
            }
        }
        catch { }

        return new DeviceTelemetryDto(serial, carrier, network, signalDbm, battery, apn ?? "v-internet", iccid, wanIp);
    }

    public async Task<bool> RotateDeviceIpAsync(string serial, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteShellAsync(serial, "cmd connectivity airplane-mode enable", cancellationToken);
            await Task.Delay(1500, cancellationToken);
            await ExecuteShellAsync(serial, "cmd connectivity airplane-mode disable", cancellationToken);
            return true;
        }
        catch
        {
            try
            {
                await ExecuteShellAsync(serial, "svc data disable", cancellationToken);
                await Task.Delay(1500, cancellationToken);
                await ExecuteShellAsync(serial, "svc data enable", cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private async Task<string> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executablePath, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start adb.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var standardError = process.StandardError.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);
        var error = await standardError;
        var output = await standardOutput;
        if (process.ExitCode != 0) throw new IOException($"ADB failed ({process.ExitCode}): {error.Trim()}");
        return output;
    }

    private static AdbDevice? ParseDevice(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        string? Field(string key) => parts.FirstOrDefault(x => x.StartsWith(key + ":", StringComparison.Ordinal))?[((key.Length + 1)..)];
        return new(parts[0], parts[1], Field("model"), Field("product"), null);
    }

    private static string Quote(string value) => '"' + value.Replace("\"", "\\\"", StringComparison.Ordinal) + '"';
}
