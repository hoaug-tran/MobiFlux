# Changes: MobiFlux

| ID | Request | Schedule impact | Cost / effort impact | Risk impact | Quality impact | Options | Decision | Status |
|---|---|---|---|---|---|---|---|---|
| CH1 | Standardize all public API routes under `/api/v1/mobiflux`, replace direct defaults with environment configuration, and complete remaining platform modules/UI. | No committed external date exists (A1); expands the internal M4 delivery range by 8–20 implementation increments (A4). | 8–20 increments, expert judgement; no local reference class (A4). | Adds compatibility, migration and untested-hardware exposure (R5–R7). | Increases consistency, security, observability and testability; deprecated unversioned routes are removed rather than retained ambiguously. (user, 2026-09-26) | Absorb: no, exceeds M4 buffer. Trade: defer modules. Extend: build the full backlog in ordered increments. Reject: no. | Accepted by Project owner (user, 2026-09-26). | decided; propagation in progress |
