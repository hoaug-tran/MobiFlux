using Microsoft.EntityFrameworkCore;
using MobiFlux.Application.Abstractions;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Infrastructure.Persistence;

public sealed class EfMobiFluxRepository(MobiFluxDbContext db) : IDeviceRepository, IProxyRepository, IProxySessionRepository
{
    public Task<DeviceNode?> GetAsync(Guid id, CancellationToken token) => db.Devices.SingleOrDefaultAsync(x => x.Id == id, token);
    public Task<DeviceNode?> GetByAdbSerialAsync(string serial, CancellationToken token) => db.Devices.SingleOrDefaultAsync(x => x.AdbSerial == serial, token);
    public async Task<IReadOnlyList<DeviceNode>> ListAsync(CancellationToken token) => await db.Devices.OrderBy(x => x.Name).ToListAsync(token);
    public Task AddAsync(DeviceNode device, CancellationToken token) => db.Devices.AddAsync(device, token).AsTask();
    public Task<ProxyEndpoint?> GetEndpointAsync(Guid id, CancellationToken token) => db.ProxyEndpoints.SingleOrDefaultAsync(x => x.Id == id, token);
    public async Task<IReadOnlyList<ProxyEndpoint>> ListEndpointsAsync(CancellationToken token) => await db.ProxyEndpoints.OrderBy(x => x.Name).ToListAsync(token);
    public Task AddEndpointAsync(ProxyEndpoint endpoint, CancellationToken token) => db.ProxyEndpoints.AddAsync(endpoint, token).AsTask();
    public Task<ProxyPool?> GetPoolAsync(Guid id, CancellationToken token) => db.ProxyPools.Include(x => x.Members).SingleOrDefaultAsync(x => x.Id == id, token);
    public async Task<IReadOnlyList<ProxyPool>> ListPoolsAsync(CancellationToken token) => await db.ProxyPools.Include(x => x.Members).OrderBy(x => x.Name).ToListAsync(token);
    public Task AddPoolAsync(ProxyPool pool, CancellationToken token) => db.ProxyPools.AddAsync(pool, token).AsTask();
    public Task SaveChangesAsync(CancellationToken token) => db.SaveChangesAsync(token);
    public async Task AppendBatchAsync(IReadOnlyCollection<ProxySession> sessions, CancellationToken token)
    {
        if (sessions.Count == 0) return;
        await db.ProxySessions.AddRangeAsync(sessions, token);
        await db.SaveChangesAsync(token);
    }
    public async Task<ProxySessionPage> GetPageAsync(ProxySessionFilter filter, CancellationToken token)
    {
        var query = db.ProxySessions.AsNoTracking().AsQueryable();
        if (filter.EndpointId is { } endpointId) query = query.Where(session => session.EndpointId == endpointId);
        if (filter.DeviceId is { } deviceId) query = query.Where(session => session.DeviceId == deviceId);
        var totalCount = await query.CountAsync(token);
        var items = await query.OrderByDescending(session => session.EndedAt).ThenByDescending(session => session.Id)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(token);
        return new ProxySessionPage(items, filter.Page, filter.PageSize, totalCount);
    }
    public async Task<ProxyTrafficSummary> GetSummaryAsync(CancellationToken token)
    {
        var summary = await db.ProxySessions.AsNoTracking().GroupBy(_ => 1)
            .Select(group => new { Count = group.Count(), BytesUp = group.Sum(session => session.BytesUp), BytesDown = group.Sum(session => session.BytesDown), MostRecentSessionAt = group.Max(session => (DateTimeOffset?)session.EndedAt) })
            .SingleOrDefaultAsync(token);
        return summary is null ? new ProxyTrafficSummary(0, 0, 0, null) : new ProxyTrafficSummary(summary.Count, summary.BytesUp, summary.BytesDown, summary.MostRecentSessionAt);
    }
}
