# Delivery plan: MobiFlux

| Milestone | Scope / acceptance evidence | Dependencies | Owner | Estimate | Status |
|---|---|---|---|---|---|
| M1 Foundation | Solution builds; domain/application/contracts, SQLite control-plane model, API and desktop shell exist. EV: build output and automated checks. | none | Project owner | 2–5 days (A1) | green (EV7) |
| M2 Device control | ADB discovery differentiates authorized/offline/unauthorized and reconciliation persists desired state. EV: attached Android test. | M1, A3 | Project owner | 1–3 days (A3) | not started |
| M3 Cellular exit | Agent opens remote DNS and sockets only through `Network` with `TRANSPORT_CELLULAR`; missing network rejects connection. EV: real-device end-to-end test. | M2, A2 | Project owner | 3–8 days (A2) | not started |
| M4 Operations | SOCKS5 + CONNECT endpoints, pools/sticky routing, health/incidents, WPF management and route verification operate through M3. EV: acceptance tests 1–10 where applicable. | M3 | Project owner | 2–6 days (A1) | not started |
| M5 API/configuration baseline | All public controls use versioned REST resources; Development/Production/env configuration, configuration validation, secrets boundary, structured logging and error contract are verified. EV: route/config integration checks. | M1 | Project owner | 1–3 increments (A4, CH1) | amber — v1 route/config build verified (EV6); remaining resources and secret-negative checks pending |
| M6 Observability/lifecycle | Sessions, traffic rollups, health incidents, audit, diagnostics and retention are persisted and surfaced. EV: integration tests. | M5 | Project owner | 2–5 increments (A4, CH1) | amber — bounded session persistence and traffic/session REST reads work (EV7); health, audit, diagnostics and retention remain |
| M7 Network operations | Rotation, recovery, pool eligibility/failover, credentials and LAN ACL are capability-gated and tested. EV: simulated and device tests. | M2, M3, M5 | Project owner | 2–5 increments (A4, CH1) | not started |
| M8 Product UI | All approved operator pages consume live versioned APIs; i18n/theme/accessibility/error/loading states are verified. EV: UI acceptance checklist. | M5, M6, M7 | Project owner | 2–4 increments (A4, CH1) | amber — dashboard consumes traffic/session API and starts in configured EN/VI resources (EV7); operator pages and acceptance checklist remain |
| M9 Security | RBAC/API key/secret protection and audit coverage pass negative tests. EV: security test suite. | M5–M8 | Project owner | 1–3 increments (A4, CH1) | not started |
| M10 Hardening | Soak, restore, device disconnect, carrier/IP, and route-leak acceptance tests pass. EV: test artifacts. | M2–M9 | Project owner | 1–4 increments (A2–A4, CH1) | not started |

Critical path: M1 → M2 → M3 → M5 → M6 → M7 → M8/M9 → M10. Visible buffers are in `plan/estimates.md`; R1–R7 drive M2–M10.

## Judge audit — 2026-09-26

- Pass with open findings: every milestone has an owner, dependency, estimate label and acceptance evidence; no external date is invented.
- Finding: M2–M4 cannot receive a truthful RAG state until hardware evidence exists (EV2, A2, A3). The plan keeps them unstarted rather than green.
- Pre-mortem incorporated: unauthorized ADB (R1), cellular routing incompatibility (R2), carrier address variation (R3), and unsafe LAN exposure (R4).
