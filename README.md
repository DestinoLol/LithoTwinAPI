# LithoTwinAPI

Industrial digital twin in .NET 7 simulating EUV (Extreme Ultraviolet) lithography scanners. The system models machine lifecycle state transitions, causal equipment fault propagation, thermal physics, wafer batch routing, and deterministic overlay error calculations.

---

## Architecture & System Overview

LithoTwinAPI is built around domain modeling without infrastructure coupling in the domain layer. The project is organized into dedicated layers:

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

Each EUV scanner operates under a Finite State Machine (FSM) enforcing industrial operational sequences:

```text
         ┌─────────────┐
         │    Idle     │�-�─────────────────┐
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

---

## API Reference

### 1. Factory Management (`/api/factory`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/factory/system-status` | Overview of all machines, states, and runtime stats |
| `GET` | `/api/factory/machines/{id}/health` | Weighted health score (0–100) and metric breakdown |
| `GET` | `/api/factory/machines/{id}/maintenance-prediction` | Maintenance urgency and overlay degradation trend |
| `GET` | `/api/factory/machines/compare?ids=A,B` | Side-by-side comparison and optimal machine recommendation |
| `POST` | `/api/factory/machines/{id}/transition?targetState=&reason=` | Trigger validated state machine transition |
| `GET` | `/api/factory/machines/{id}/transitions` | Audit history of lifecycle transitions |
| `POST` | `/api/factory/machines/{id}/fault?faultType=&description=` | Inject simulated equipment fault |
| `POST` | `/api/factory/machines/{id}/resolve-faults` | Clear active faults (Maintenance state required) |
| `GET` | `/api/factory/machines/{id}/faults` | List active unresolved faults |
| `POST` | `/api/factory/telemetry?machineId=&temperature=` | Ingest sensor reading with fault-aware validation |
| `GET` | `/api/factory/telemetry/{id}/history` | Retrieve historical telemetry readings |
| `GET` | `/api/factory/telemetry/{id}/trend` | Compute disjoint window trend (`rising`, `falling`, `stable`) |
| `GET` | `/api/factory/telemetry/{id}/export` | Export machine telemetry as invariant CSV |
| `POST` | `/api/factory/route-wafer` | Route wafer batch to coldest available Running machine |
| `POST` | `/api/factory/batches/{id}/complete` | Complete wafer batch and increment wafer count |
| `GET` | `/api/factory/alerts` | List unacknowledged system alerts |
| `POST` | `/api/factory/alerts/{id}/acknowledge` | Acknowledge alert |
| `GET` | `/api/factory/stats` | Aggregate factory production, machine, and alert metrics |

### 2. EUV Exposure (`/api/exposure`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/exposure/run` | Execute EUV shot, compute overlay error, and log stage heat |
| `GET` | `/api/exposure/history?machineId=` | Retrieve last 100 exposure results for a machine |

### 3. Reticle Management (`/api/reticle`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/reticle` | List all reticles with usage and contamination levels |
| `GET` | `/api/reticle/{id}` | Get reticle details |
| `POST` | `/api/reticle/{id}/inspect` | Simulate inspection, update contamination, and check usability |

### 4. Real-time Monitoring (`/api/live`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/live/alerts` | Server-Sent Events (SSE) stream delivering real-time alerts |

---

## Build & Execution

### Requirements
- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)

### Quickstart

```bash
# Clone and navigate to repository
git clone https://github.com/DestinoLol/LithoTwinAPI.git
cd LithoTwinAPI

# Restore dependencies and build
dotnet restore
dotnet build --no-restore

# Run automated tests (xUnit)
dotnet test

# Run API application
dotnet run
```

### Swagger UI
Once running, interactive API documentation is available at:
`http://localhost:5159/swagger`

![Swagger Endpoints](docs/swagger_endpoints.png)

### Persistence Configuration
By default, the application runs using EF Core `InMemory` provider with no external database dependencies. To persist data to a local SQLite database, set `"UseSqlite": true` in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "UseSqlite": true,
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=lithotwin.db"
  }
}
```

---

## License
This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
