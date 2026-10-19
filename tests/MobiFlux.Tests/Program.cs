using MobiFlux.Application.Proxies;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;

if (args.Length is 2 or 3 && args[0] == "--verify-backup")
{
    var backupPath = args[1];
    await using var backup = new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = backupPath, Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
    await backup.OpenAsync();
    await using var integrity = backup.CreateCommand();
    integrity.CommandText = "PRAGMA integrity_check";
    if (!Equals(await integrity.ExecuteScalarAsync(), "ok")) throw new IOException("Backup integrity check failed");
    integrity.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('devices','proxy_endpoints','proxy_sessions')";
    if (Convert.ToInt32(await integrity.ExecuteScalarAsync()) != 3) throw new IOException("Backup schema missing required tables");
    if (args.Length == 3)
    {
        integrity.CommandText = "SELECT COUNT(*) FROM devices WHERE Id=$id";
        integrity.Parameters.AddWithValue("$id", Guid.Parse(args[2]));
        if (Convert.ToInt32(await integrity.ExecuteScalarAsync()) != 1) throw new IOException("Backup missing expected device");
    }
    Console.WriteLine("PASS: SQLite backup integrity and required schema");
    return 0;
}

var failures = new List<string>();
void Check(bool value, string name) { if (!value) failures.Add(name); }

try { _ = new ProxyEndpoint("bad", "127.0.0.1", 12000, ProxyProtocol.Socks5, null, null, DnsMode.RemoteOnly, 1); failures.Add("endpoint requires a target"); }
catch (ArgumentException) { }

var device = new DeviceNode("serial-1", "Phone 01", DateTimeOffset.UtcNow);
var capabilities = new DeviceCapabilities { DeviceId = device.Id, CanProxyTcp = true, CanResolveDnsOnCellular = true };
Check(!DeviceEligibility.CanProxy(device, capabilities), "disabled device is ineligible");
device.Enable(DateTimeOffset.UtcNow); device.Reconcile(DeviceActualState.ProxyReady, DateTimeOffset.UtcNow);
Check(DeviceEligibility.CanProxy(device, capabilities), "ready device with cellular DNS is eligible");
var noDnsCapabilities = new DeviceCapabilities { DeviceId = device.Id, CanProxyTcp = true, CanResolveDnsOnCellular = false };
Check(!DeviceEligibility.CanProxy(device, noDnsCapabilities), "missing cellular DNS fails closed");

foreach (var strategy in new[] { RoutingStrategy.RoundRobin, RoutingStrategy.WeightedRoundRobin })
{
    var a = new DeviceNode("route-a", "A", DateTimeOffset.UtcNow);
    var b = new DeviceNode("route-b", "B", DateTimeOffset.UtcNow);
    foreach (var node in new[] { a, b }) { node.Enable(DateTimeOffset.UtcNow); node.Reconcile(DeviceActualState.ProxyReady, DateTimeOffset.UtcNow); }
    var pools = Enumerable.Range(0, 2).Select(index => new ProxyPool($"pool-{index}", strategy, 0)).ToArray();
    foreach (var pool in pools) { pool.AddMember(a.Id, 1, 0); pool.AddMember(b.Id, 1, 0); }
    var endpoints = pools.Select(pool => new ProxyEndpoint("route", "127.0.0.1", 12000, ProxyProtocol.Socks5, null, pool.Id, DnsMode.RemoteOnly, 10)).ToArray();
    var resolver = new MobiFlux.Infrastructure.Routing.CachedRouteResolver(TimeProvider.System, 100);
    resolver.Refresh(new[] { a, b }, pools);
    var first = await resolver.ResolveAsync(endpoints[0], "client", default);
    await resolver.ResolveAsync(endpoints[1], "client", default);
    var second = await resolver.ResolveAsync(endpoints[0], "client", default);
    Check(first is not null && second is not null && first.DeviceId != second.DeviceId, $"{strategy} counters isolated per pool");
}

await ProxyFlowChecks.RunAsync(failures);
await ReconciliationChecks.RunAsync(failures);
if (args.Length == 2 && args[0] == "--hub") await LiveHubChecks.RunAsync(args[1], failures);
if (failures.Count > 0) { Console.Error.WriteLine(string.Join(Environment.NewLine, failures)); return 1; }
Console.WriteLine("MobiFlux domain, proxy flow and reconciliation checks passed."); return 0;
