# LithoTwinAPI

Industrial digital twin in .NET 7 simulating EUV (Extreme Ultraviolet) lithography scanners. The system models machine lifecycle state transitions, causal equipment fault propagation, thermal physics, wafer batch routing, and deterministic overlay error calculations.

---

## Architecture & System Overview

LithoTwinAPI is built around strict domain modeling with zero infrastructure coupling in the domain layer. The project is organized into dedicated layers:

```text
LithoTwinAPI/
├── Controllers/       # REST API and SSE endpoints
├── Domain/            # Pure C# domain logic (FSM, invariants, named constants)
├── Services/          # Behavioral orchestration and business logic
├── Models/            # Domain entities, DTOs, and lifecycle states
├── Simulation/        # Background thermal drift engine and physics formulas
└── Data/              # EF Core persistence (InMemory default, SQLite opt-in)
```

### Machine Lifecycle State Machine

Each EUV scanner operates under a strict Finite State Machine (FSM) enforcing industrial operational sequences:

```text
         ┌─────────────┐
         │    Idle     │◄─────────────────┐
         └──────┬──────┘                  │
                │ Calibrate               │
                ▼                         │
         ┌─────────────┐                  │
         │ Calibrating │                  │
         └──────┬──────┘                  │
                │ Warmup complete         │
                ▼                         │
         ┌─────────────┐ Fault detected   │
         │   Running   ├──────────┐       │
         └──────┬──────┘          │       │
                │ Maintenance due │       │
                ▼                 ▼       │
         ┌─────────────┐   ┌─────────────┐│
         │ Maintenance │   │   Faulted   ││
         └──────┬──────┘   └──────┬──────┘│
                │                 │       │
                │ Recovered       │ Repair│
                └─────────────────┴───────┘
```

#### Transition Rules:
- **Idle $\rightarrow$ Calibrating**: Scanner prepares optics and stage alignment.
- **Calibrating $\rightarrow$ Running**: Calibration passes and scanner enters production.
- **Running $\rightarrow$ Maintenance / Faulted**: Scheduled service or automatic fault shutdown.
- **Faulted $\rightarrow$ Maintenance**: Faults cannot transition directly back to Running; repairs must occur in Maintenance.
- **Maintenance $\rightarrow$ Idle**: System restored to baseline nominal state.

---

## Physical Modeling & Invariants

### 1. Causal Fault Propagation
Equipment faults are not purely aesthetic; they causally alter physical telemetry and wafer quality:
- `ThermalOverload`: Injects +0.5°C drift spike per simulation tick.
- `LaserDegradation`: Reduces throughput by 30% and degrades overlay alignment.
- `SensorFailure`: Injects ±2.0°C measurement noise into telemetry streams.

### 2. Deterministic Overlay Error
Total overlay error ($E_{\text{overlay}}$) is computed from stage temperature, focus offsets, and active degradation:

$$E_{\text{overlay}} = \sqrt{E_x^2 + E_y^2}$$

$$\text{Overlay} = \left( \Delta T \times k_{\text{thermal}} + |\Delta f| \times k_{\text{focus}} \right) \times f_{\text{degradation}} + \text{noise}$$

If total overlay exceeds `OverlaySpecLimitNm` (1.5nm), the exposure fails and generates a quality alert.
