# Estimates: MobiFlux

All ranges are implementation effort ranges, not calendar commitments. No project reference class exists yet (A1).

| Item | Basis | Low–High | Reference class | Buffer | Label |
|---|---|---|---|---|---|
| M1 Foundation and control plane | bottom-up | 2–5 days | none | 1 day integration buffer | A1 |
| M2 ADB discovery and reconciliation | expert judgment | 1–3 days | none | 1 day device variance buffer | A3 |
| M3 Android cellular agent and direct tunnel | expert judgment | 3–8 days | none | 2 days real-device compatibility buffer | A2 |
| M4 Gateway, pooling, health and desktop operations | bottom-up | 2–6 days | none | 1 day integration buffer | A1 |
| M5 API/configuration/security baseline | bottom-up | 1–3 increments | none | 1 increment compatibility/security buffer | A4, CH1 |
| M6 Observability and lifecycle | bottom-up | 2–5 increments | none | 1 increment integration buffer | A4, CH1 |
| M7 Pool/rotation/recovery and LAN controls | expert judgment | 2–5 increments | none | 1–2 increments device/network buffer | A4, CH1, R1–R4 |
| M8 Product management surfaces | bottom-up | 2–4 increments | none | 1 increment UX/API parity buffer | A4, CH1, R7 |
| M9 Security and operator controls | bottom-up | 1–3 increments | none | 1 increment review buffer | A4, CH1, R6 |
| M10 Acceptance and hardening | expert judgment | 1–4 increments | none | 1 increment device soak buffer | A2–A4, CH1 |

Re-estimate after a real Android device completes M3.
