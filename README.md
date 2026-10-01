# MobiFlux

Local-first orchestration for Android cellular proxy nodes on Windows. It is intentionally a modular monolith: the control plane persists configuration in SQLite while the data plane moves streams directly and never uses EF Core per packet.

## What is implemented

- Clean `.NET 10` solution boundaries: Domain, Application, Infrastructure, Proxy, Service, Desktop and Contracts.
- SQLite-backed devices, proxy endpoints and pools; feature-slice command handlers and a local HTTP/SignalR control API.
- ADB discovery/reconciliation with explicit `offline` and `unauthorized` states.
- Android foreground agent which binds a loopback-only tunnel to an Android `TRANSPORT_CELLULAR` `Network` for both DNS and sockets. No cellular network means an explicit failure — it never opens a default/Wi-Fi socket.
- SOCKS5 TCP and HTTP CONNECT listeners, direct device and sticky/round-robin pool selection, and stream forwarding outside the database hot path.
- Loopback-only service and proxy defaults; a non-loopback proxy listener cannot start without an explicit LAN security implementation.
- WPF operator shell, domain checks, project plan/risk registers, and a real local service smoke test.

## Run on Windows

```powershell
dotnet restore MobiFlux.sln --configfile NuGet.Config
dotnet run --project src/MobiFlux.Service/MobiFlux.Service.csproj
dotnet run --project src/MobiFlux.Desktop/MobiFlux.Desktop.csproj
```

The service listens at `http://127.0.0.1:5080`, initializes `data/mobiflux.db`, and exposes `/health` plus `/api/devices`, `/api/proxies`, and `/api/pools`.

## Android agent

The agent source is in [`android`](android). It exposes only loopback ports on the phone:

- `18080`: `MFP1` cellular stream tunnel.
- `18081`: agent status used by the Windows reconciliation worker.

Windows creates USB ADB forwards to those ports. A real device must be USB-authorized and have a working SIM before any endpoint is marked `ProxyReady`.

## Safety invariant

MobiFlux does **not** claim that a listener alone is a mobile proxy. A route becomes usable only after the agent is reachable and reports cellular available. If ADB, agent, DNS, or cellular routing is absent, the connection fails closed rather than falling back to Windows Ethernet/Wi-Fi or Android Wi-Fi.

The real-device acceptance checklist and phased delivery plan are maintained under [`_project/plan/schedule.md`](_project/plan/schedule.md).
