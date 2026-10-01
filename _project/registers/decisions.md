# Decisions: MobiFlux

## 2026-09-26 — Start as a local-first modular monolith
- **Decision:** implement MobiFlux naming and the solution boundaries proposed by the project owner; use direct stream data-plane code and keep control-plane persistence outside the connection hot path.
- **Alternatives considered:** an early distributed or SaaS implementation.
- **Why:** the supplied plan explicitly prioritizes 1–10 phones on one Windows host and identifies a no-fallback cellular route invariant (EV1).
- **Revisit when:** the first real-device acceptance test succeeds and a scale requirement above one Windows node is confirmed.

## 2026-09-26 — Treat Android/device acceptance as a capability-gated integration
- **Decision:** implement the agent and Windows transport but do not claim the system routes through a SIM until a physical-device test produces evidence.
- **Alternatives considered:** mock a successful rotation or egress result.
- **Why:** current workspace evidence has no attached Android hardware (EV2); A2 and R2 make a simulated success misleading.
- **Revisit when:** a USB-authorized device and SIM are available.

## 2026-09-26 — Ship the local foundation with fail-closed gates intact
- **Decision:** retain the implemented M1/M2/M4 code while leaving M3 acceptance explicitly pending rather than providing a simulated cellular success.
- **Alternatives considered:** make the Windows gateway connect using a normal Windows socket when the agent is absent.
- **Why:** the clean build and local API prove the foundation (EV3, EV4), but a normal host socket would violate the central route invariant (EV1) and conceal R2/R3.
- **Revisit when:** the first Android SIM test produces an end-to-end egress result.

## 2026-09-26 — Align the control plane to feature-slice CQRS
- **Decision:** replace direct minimal-API/repository calls with the `Shared → Application → Infrastructure → Service` convention: MediatR command/query feature folders, FluentValidation behavior, application ports, controller-only transport, global exception middleware and Serilog correlation logging.
- **Alternatives considered:** retain lightweight direct handlers and minimal route lambdas.
- **Why:** the requested reference architecture relies on each use case being independently discoverable and testable; the previous structure did not meet that bar (EV5).
- **Revisit when:** the remaining proxy modules are moved into feature slices and integration tests cover their error contracts.

## 2026-09-26 — Accept CH1: versioned product-platform completion
- **Decision:** expose public controls only as `/api/v1/mobiflux/...`, move deployment values to validated environment-aware configuration, and complete the remaining platform features/UI in M5–M10 rather than presenting the foundation as complete.
- **Alternatives considered:** retain unversioned APIs for convenience; preserve default source-code values; defer platform modules indefinitely.
- **Why:** the Project owner requires a production-grade local product with unambiguous public contracts and no inappropriate hardcoding (user, 2026-09-26; CH1).
- **Against advice / dissent:** no external date or hardware evidence exists, so no completion date or 100% claim is recorded (A2–A4).
- **Revisit when:** M5 route/config integration tests and each following milestone's acceptance evidence are complete.

## 2026-09-26 — Keep telemetry out of the proxy forwarding hot path
- **Decision:** emit completed proxy sessions to a bounded, non-blocking queue; persist them in configured batches with retry and expose only paged session/aggregate traffic reads to the dashboard.
- **Alternatives considered:** write to SQLite synchronously for every connect/close; present fabricated live metrics in the desktop dashboard.
- **Why:** forwarding latency must not depend on storage availability, while operators still need durable, queryable evidence (user, 2026-09-26; EV7).
- **Against advice / dissent:** the bounded queue intentionally drops telemetry on saturation rather than blocking proxy traffic; the dropped-count alert/metric is still required in M6.
- **Revisit when:** a real-device throughput soak test establishes queue capacity, loss tolerance, retention and alert thresholds.
