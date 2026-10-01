using Microsoft.EntityFrameworkCore;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Monitoring;
using MobiFlux.Domain.Proxies;

namespace MobiFlux.Infrastructure.Persistence;

public sealed class MobiFluxDbContext(DbContextOptions<MobiFluxDbContext> options) : DbContext(options)
{
    public DbSet<DeviceNode> Devices => Set<DeviceNode>();
    public DbSet<DeviceCapabilities> DeviceCapabilities => Set<DeviceCapabilities>();
    public DbSet<ProxyEndpoint> ProxyEndpoints => Set<ProxyEndpoint>();
    public DbSet<ProxyPool> ProxyPools => Set<ProxyPool>();
    public DbSet<ProxyPoolMember> ProxyPoolMembers => Set<ProxyPoolMember>();
    public DbSet<ProxySession> ProxySessions => Set<ProxySession>();
    public DbSet<NetworkSnapshot> NetworkSnapshots => Set<NetworkSnapshot>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<DeviceNode>(entity => { entity.ToTable("devices"); entity.HasKey(x => x.Id); entity.HasIndex(x => x.AdbSerial).IsUnique(); });
        model.Entity<DeviceCapabilities>(entity => { entity.ToTable("device_capabilities"); entity.HasKey(x => x.DeviceId); });
        model.Entity<ProxyEndpoint>(entity => { entity.ToTable("proxy_endpoints"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.BindAddress, x.Port }).IsUnique(); });
        model.Entity<ProxyPool>(entity => { entity.ToTable("proxy_pools"); entity.HasKey(x => x.Id); entity.HasMany(x => x.Members).WithOne().HasForeignKey(x => x.PoolId); });
        model.Entity<ProxyPoolMember>(entity => { entity.ToTable("proxy_pool_members"); entity.HasKey(x => new { x.PoolId, x.DeviceId }); });
        model.Entity<ProxySession>(entity =>
        {
            entity.ToTable("proxy_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StartedAt).HasConversion(value => value.ToUnixTimeMilliseconds(), value => DateTimeOffset.FromUnixTimeMilliseconds(value));
            entity.Property(x => x.EndedAt).HasConversion(value => value.ToUnixTimeMilliseconds(), value => DateTimeOffset.FromUnixTimeMilliseconds(value));
            entity.HasIndex(x => new { x.EndedAt, x.EndpointId });
            entity.HasIndex(x => new { x.EndedAt, x.DeviceId });
        });
        model.Entity<NetworkSnapshot>(entity => { entity.ToTable("network_snapshots"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.DeviceId, x.Timestamp }); });
    }
}
