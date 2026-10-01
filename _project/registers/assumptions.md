# Assumptions: MobiFlux

| ID | Statement | Basis | Range (low–high) | Used by | Status |
|---|---|---|---|---|---|
| A1 | Initial delivery is an engineering foundation/MVP rather than all future distributed phases being production-complete in one session. | No device fleet, Android APK signing setup, production deployment target, budget, or delivery date was supplied. | 1–4 implementation increments | charter, schedule, estimates | open |
| A2 | Android agent control endpoint is exposed only through an ADB loopback forward and its binary tunnel is compatible with the Windows transport. | Required for real-device end-to-end acceptance; no device has been connected. | 1–3 days of integration investigation per device/OS family | schedule M3, R2 | open |
| A3 | ADB platform-tools is installed or the configured executable path will be supplied before a physical-device test. | Environment has .NET and Java but no verified adb executable result yet. | 0–1 day | schedule M2, R1 | open |
| A4 | Completing the full local platform after the current foundation requires 8–20 implementation increments, excluding unknown carrier/device compatibility work. | Bottom-up inventory of persistence, security, observability, routing, recovery, API, desktop UX and test work; no project reference class exists. | 8–20 increments | CH1, schedule M5–M10, estimates | open |
