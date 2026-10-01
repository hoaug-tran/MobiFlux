# Risks: MobiFlux

| ID | Risk | P | I | Score | ROAM | Owner | Trigger / next review | Status |
|---|---|---:|---:|---:|---|---|---|---|
| R1 | ADB is unavailable, unauthorized, or unstable on the target Windows/USB topology. | 3 | 4 | 12 | Owned | Project owner | Trigger: `adb devices` does not return an authorized device; review at M2. | active |
| R2 | Android vendor/OS behaviour prevents the cellular-only socket tunnel from working as designed. | 3 | 5 | 15 | Owned | Project owner | Trigger: real-device route test leaks or cannot reach the test endpoint; review at M3. | active |
| R3 | Carrier NAT/IPv6/NAT64 characteristics make an expected egress-IP assertion invalid. | 3 | 4 | 12 | Owned | Project owner | Trigger: IPv4 or IPv6 egress test differs by carrier; review at M3. | active |
| R4 | LAN listener configuration could create an unauthorized open proxy. | 2 | 5 | 10 | Owned | Project owner | Trigger: non-loopback bind requested; review before LAN enablement. | active |
| R5 | API route/version migration breaks an existing local caller that used unversioned endpoints. | 2 | 3 | 6 | Owned | Project owner | Trigger: any local client receives 404 after CH1; review before release. | active |
| R6 | Secrets or production connection settings are committed or logged by mistake during environment standardization. | 2 | 5 | 10 | Owned | Project owner | Trigger: secret-scanner or review finds an actual credential in tracked content; review before release. | active |
| R7 | Desktop information architecture expands faster than its real API/query layer, creating fake metrics or unusable screens. | 3 | 4 | 12 | Owned | Project owner | Trigger: any UI metric has no live API source; review at each UI feature slice. | active |
