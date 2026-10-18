using Microsoft.AspNetCore.SignalR.Client;
using MobiFlux.Shared.Contracts;

internal static class LiveHubChecks
{
    public static async Task RunAsync(string origin, List<string> failures)
    {
        await using var hub = new HubConnectionBuilder().WithUrl(origin.TrimEnd('/') + "/hubs/runtime").Build();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var samples = new List<RuntimeSnapshotDto>();
        hub.On<RuntimeSnapshotDto>("RuntimeUpdated", sample =>
        {
            samples.Add(sample);
            if (samples.Count >= 2) received.TrySetResult();
        });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await hub.StartAsync(timeout.Token);
        await received.Task.WaitAsync(timeout.Token);
        await hub.StopAsync(timeout.Token);
        if (samples.Count < 2 || samples[1].At <= samples[0].At || samples.Any(sample => sample.Traffic.DownloadMbps < 0 || sample.Traffic.UploadMbps < 0)) failures.Add("live SignalR samples must advance and contain valid measurements");
        Console.WriteLine($"SignalR received {samples.Count} real server samples.");
    }
}
