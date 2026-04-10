# API Changelog

Notable changes to the LithoTwin HTTP API surface and domain contracts are documented in this file.

---

## 2026-04-10 — Sprint Refinement & Diagnostics Overhaul

### Added
- **`GET /api/factory/machines/compare?ids=`**: Side-by-side comparison endpoint accepting comma-separated machine IDs. Computes composite health scores, evaluates thermal headroom, and returns recommended machine for production routing.
- **`FailureReason` on `ExposureResult`**: Detailed diagnostic message explaining which overlay threshold was violated when `Passed == false`.
- **`ReticleService` Layering**: Dedicated domain service managing reticle contamination cycles and usability lifecycle.
- **`AmbientConvergenceRatePerTick` Constant**: Dimensionless rate coefficient for passive Newton cooling towards ambient baseline in Idle state.

### Changed
- **Health Score Unification**: Consolidated scoring formula between individual `/health` and batch `/compare` endpoints into single `ComputeOverallHealthScore` helper.
- **Compare Endpoint Contract**: Switched from multi-query `?machineIds=` to comma-separated `?ids=A,B` with explicit minimum 2-ID validation and 404 on missing machine IDs.
- **Reticle Contamination Capping**: Monotonically increasing contamination capped at `MaxContaminationLevel` (1.0), with replacement threshold set to `0.85`.
- **Deterministic Idle Drift**: Removed noise from Idle state simulation to ensure stable convergence towards ambient baseline.

---

## 2026-03-10 — Initial Release

### Added
- **`FactoryController`**: Core endpoints for machine telemetry, state transitions, fault injection, and alert acknowledgments.
- **`ExposureController`**: Endpoints for EUV exposure execution and historical run queries.
- **`ReticleController`**: Endpoints for reticle inspection and usability tracking.
- **`LiveController`**: Real-time Server-Sent Events (SSE) stream on `/api/live/alerts`.
- **Lifecycle FSM**: Strict state transitions between `Idle`, `Calibrating`, `Running`, `Faulted`, and `Maintenance`.
