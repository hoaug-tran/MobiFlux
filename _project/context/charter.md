# Charter: MobiFlux

- **Objective:** Build a local-first Windows mobile-proxy management platform whose proxy traffic exits through a selected Android device's cellular network and fails closed when that route cannot be proven. (EV1)
- **Scope in:** Windows service/API, local SQLite control plane, ADB discovery/reconciliation, Android cellular agent protocol, SOCKS5 TCP, HTTP CONNECT, direct endpoints, pools, health/runtime telemetry, WPF operations shell, route-leak protection and automated unit checks. (EV1)
- **Scope out for initial delivery:** public SaaS, billing, public ADB, GPS/fingerprint spoofing, TUN/UDP full tunnel, distributed node manager. (EV1)
- **Success criteria:** a configured endpoint completes an end-to-end proxy test through Android cellular; loss of cellular rejects new connections; a device reconnect reconciles without changing persisted endpoint mapping. (EV1)
- **Sponsor / decision authority:** Project owner. (user, 2026-09-26)
- **Commitment:** no external delivery date or budget supplied. This is an engineering baseline, not a calendar commitment. (A1)

Revisit when: scope is approved for production rollout, a real-device acceptance result arrives, or an external date is committed.
