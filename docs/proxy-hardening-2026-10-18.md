# Proxy hardening — 2026-10-18

## Implemented in this change

- Preserve a response after the client half-closes its upload. Both pumps are awaited, transport failures cancel the peer pump, and shutdown waits for client tasks.
- Count bytes after successful writes, separately for upload and download. Close the session even when the success reply fails.
- Enforce MaxConnections on accepted clients, serialize start/stop operations, expire incomplete handshakes and inactive transfers using configured timeouts.
- Preserve sticky identity across connections from the same source IP. On loopback, all local clients intentionally share that identity; separate endpoints are required for independent sticky identities.
- Reject malformed SOCKS requests and handle bracketed IPv6 CONNECT destinations. Never append a second protocol response after the tunnel has started.
- Require structured agent status with cellularAvailable, canProxyTcp, canResolveDnsOnCellular and failClosed before marking a device ProxyReady. Reconciliation refreshes cached routes, and worker failures clear the cache.
- Replace TCP-only proxy tests with HTTPS through the selected proxy and IP egress parsing. Probe commands have validators and handlers; transport belongs to Infrastructure.
- Move rotation and history behind CQRS. History is bounded to 500 entries and persisted atomically beside the database. Reject ambiguous multi-device rotation. Serialize reconnect requests, restore connectivity in finally even on cancellation, and distinguish a reconnect request from a verified IP change.
- Remove fabricated signal/APN values, SIM-state-as-ICCID, and default-route-source-as-WAN-IP. Unknown values remain null.
- Parse tab-separated ADB output correctly, terminate timed-out ADB child processes, and allocate the first forward at the configured base port.
- Android: add missing network permissions, bound handshake reads, cap accepted tunnels, close active sockets on stop, try cellular-resolved addresses with connect deadlines, and retain buffered bytes across handshake/stream forwarding.
- Use connectedDevice foreground service for the Windows companion connection. This matches external USB/network interactions described by [Android foreground service documentation](https://developer.android.com/develop/background-work/services/fgs/service-types?hl=en). Device/OEM behavior remains unverified.

## Evidence

Before the transfer fix, the new socket tests failed for both protocols with EndOfStreamException. After the fix, domain, socket flow and reconciliation checks pass.

Commands:

```powershell
dotnet restore MobiFlux.sln --configfile NuGet.Config
dotnet build MobiFlux.sln --no-restore
dotnet run --project tests/MobiFlux.Tests --no-restore
pwsh -File tests/Test-ControlPlane.ps1
```

.NET build: zero warnings/errors. The control-plane smoke test verifies health 200, no devices in its isolated database, unreachable closed proxy with no egress IP, and HTTP 400 for non-loopback quick tests. Temporary test databases/logs are isolated from the configured application database.

`adb devices -l` returned no attached devices. Gradle offline assembleDebug reached Android task dependency resolution but failed because no Android SDK was configured/found. No APK, installation, native Android runtime, or real 4G/5G egress is certified by these host checks.

## Capability boundary

| Area | Current behavior | Remaining acceptance/work |
|---|---|---|
| TCP data plane | SOCKS5 CONNECT and HTTP CONNECT, half-close, limits/timeouts | High-concurrency soak, long-lived sessions on real phones |
| Cellular routing | Agent uses cellular Network for DNS and sockets; failures reject traffic | Wi-Fi enabled + SIM IP comparison; cellular loss and recovery; IPv4/IPv6 operator tests |
| Discovery | ADB states and agent capabilities determine readiness | Agent install/update/start workflow, per-device diagnostics, prompt snapshot invalidation on every mutation |
| Endpoint management | Create/list/update/delete/start/stop, credentials and HTTPS egress tests | Import/export validation and desired runtime restoration |
| Pools | Strategies, sticky selection and membership updates | Pool deletion, per-pool counters and explicit session identity |
| Rotation | Explicit device selection, persisted history and pre/post cellular egress probes | Real SIM validation, cooldown and carrier-specific support |
| Recovery | Periodic discovery/probe and fail-closed cache clearing | Backoff, circuit breakers, automatic agent restart and persistent health events |
| Telemetry | Persisted sessions, active connections and live SignalR traffic samples | Time windows, quotas, retention and correlated diagnostics |
| Security | SOCKS/HTTP authentication with salted password hashes, explicit LAN client ACLs | TLS and control API authorization |
| Desktop | Responsive WPF shell, live traffic chart, configuration forms and saved preferences | Final visual validation of dropdown/input fixes and real-device workflows |
| Operations | Build and automated host checks | Backup/restore, packaged Windows service and signed distributions, Android SDK/APK pipeline |

## Real-device acceptance sequence

1. Configure Android SDK locally, build APK, install it and start the agent visibly on a USB-authorized phone.
2. Enable mobile data with a working SIM. Verify agent status through ADB forwarding, then ProxyReady.
3. Create a loopback endpoint targeting that device, start it, and run the proxy HTTPS test.
4. Compare observed egress with a cellular-bound reference; enable Android Wi-Fi and confirm the proxy still uses cellular.
5. Disable mobile data or disconnect USB during traffic: requests must fail without Windows/Android Wi-Fi fallback. Restore connectivity and verify new sessions recover.
6. Exercise upload half-close, large downloads/uploads, IPv6, timeout, endpoint stop, service shutdown and phone sleep/wake.
7. Run sustained parallel sessions and inspect actual session counts/traffic and phone thermal/battery behavior.

This report describes a hardened foundation, not a completed production feature set.
