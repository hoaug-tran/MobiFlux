using Microsoft.Extensions.Options;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Proxies;
using MobiFlux.Shared.Configuration;

namespace MobiFlux.Service.Services;

public sealed class ProxySessionPersistenceWorker(IServiceScopeFactory scopeFactory, IProxySessionQueue queue, IOptions<SessionPersistenceOptions> options, ILogger<ProxySessionPersistenceWorker> logger) : BackgroundService
{
    private readonly SessionPersistenceOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<ProxySession>(_options.PersistenceBatchSize);
        while (await queue.Reader.WaitToReadAsync(stoppingToken))
        {
            batch.Clear();
            Drain(batch);
            if (batch.Count < _options.PersistenceBatchSize)
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PersistenceIntervalSeconds), stoppingToken);
                Drain(batch);
            }
            await PersistWithRetryAsync(batch, stoppingToken);
        }
    }

    private void Drain(ICollection<ProxySession> batch)
    {
        while (batch.Count < _options.PersistenceBatchSize && queue.Reader.TryRead(out var session)) batch.Add(session);
    }

    private async Task PersistWithRetryAsync(IReadOnlyCollection<ProxySession> batch, CancellationToken stoppingToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IProxySessionRepository>();
                await repository.AppendBatchAsync(batch, stoppingToken);
                return;
            }
            catch (Exception exception) when (attempt < _options.PersistenceRetryCount && !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Session telemetry persistence failed on attempt {Attempt}; retrying {SessionCount} sessions.", attempt + 1, batch.Count);
                await Task.Delay(TimeSpan.FromSeconds(_options.PersistenceRetryDelaySeconds), stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Session telemetry persistence exhausted retries; {SessionCount} sessions were not persisted.", batch.Count);
                return;
            }
        }
    }
}
