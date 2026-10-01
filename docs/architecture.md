# MobiFlux architecture

MobiFlux follows the same source layout convention requested from Unetionline:

```text
src/
  MobiFlux.Shared/          API envelopes, DTOs, pagination
  MobiFlux.Domain/          entities, invariants, value types, no I/O
  MobiFlux.Application/
    Common/Behaviors/       MediatR validation and request logging pipelines
    Interfaces/             repository/runtime ports
    Features/<area>/<use-case>/
                             Command or Query + Handler + Validator
  MobiFlux.Infrastructure/  EF Core SQLite, ADB, Android transport, runtime adapters
  MobiFlux.Proxy/           socket/data plane only; no EF/CQRS per stream
  MobiFlux.Service/         controllers, middleware, Windows host, workers
  MobiFlux.Desktop/         WPF MVVM operator dashboard and design tokens
```

## Rules enforced

- API controllers dispatch an application command/query through `ISender`; no controller calls a repository or the proxy gateway.
- Every externally supplied command has FluentValidation before its handler.
- The application owns ports (`IDeviceRepository`, `IProxyRepository`, `IProxyRuntime`); Infrastructure implements them.
- Global exception handling maps known errors to stable API codes and always returns a correlation ID.
- Serilog owns request and structured file logging; data-plane sessions remain off the database hot path.
- The proxy gateway binds loopback only. A route unavailable through Android cellular fails closed.

## Current feature truth

Implemented: device registration/reconciliation, endpoint CRUD/start/stop, direct and pool selection in the data plane, Android cellular tunnel protocol, standard error/logging pipeline, and dashboard overview.

Still required before calling the product feature-complete: persisted health/session/traffic/audit models, SOCKS/HTTP authentication, DPAPI credentials, rotation/recovery/retry policies, full pool CRUD/sticky/failover policy management, LAN ACL/rate limits, RBAC/API keys, backup/restore, analytics and all desktop detail pages. These remain explicit roadmap work, not hidden behind a “done” claim.
