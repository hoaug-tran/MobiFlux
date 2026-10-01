using MobiFlux.Application.Proxies;
using MobiFlux.Domain.Devices;
using MobiFlux.Domain.Proxies;

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

if (failures.Count > 0) { Console.Error.WriteLine(string.Join(Environment.NewLine, failures)); return 1; }
Console.WriteLine("MobiFlux domain checks passed."); return 0;
