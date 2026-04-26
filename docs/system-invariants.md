# System Invariants

This document outlines the structural constraints of the system. These rules are strictly enforced in code. If any of these conditions are violated, the system is considered to be in an invalid state.

## Machine Lifecycle

State changes for machines must exclusively go through the MachineStateMachine. Direct assignment of the machine state is prohibited because it bypasses validation.

The state transitions are strictly governed:
- A machine in the Faulted state cannot transition directly to Running or Idle; it must first go to Maintenance to resolve the fault.
- A machine in the Running state cannot transition directly to Idle without passing through Maintenance or Calibrating.
- A machine in Maintenance must undergo Calibrating before it can transition back to Running.

Every state transition produces an immutable audit record containing the source state, destination state, reason, and timestamp.

## Faults

When a fault is injected into a running machine, it automatically forces a transition to the Faulted state. Faults are never silent and must persist until they are explicitly resolved. Fault resolution is only permitted when the machine is in Maintenance.

Each fault type has a deterministic effect on the system:
- ThermalOverload increases temperature readings by +0.5°C per simulation tick and is persisted directly in the telemetry.
- LaserDegradation reduces the throughput factor by 30% and degrades overlay accuracy.
- SensorFailure injects ±2°C noise into telemetry ingestion and drift measurements.

Once a fault is resolved, the throughput factor is restored to its nominal value.

## Telemetry

Telemetry ingestion is rejected for machines that are under maintenance. Any sensor readings outside the plausible range of [-10°C, 80°C] are rejected as invalid.

Telemetry output is computed as a function of the machine state and any active faults. This ensures all telemetry is causally explainable. A SensorFailure fault, for instance, injects noise into recorded temperature values, meaning the recorded value may differ from the actual sensor input.

## Exposure

Exposures can only be performed on machines that are actively running.

The overlay error is computed deterministically using the temperature, focus offset, and throughput factor. If an exposure exceeds the overlay specification limit (1.5nm), the system generates a warning alert and marks the exposure as failed with a descriptive reason.

## Routing

Wafer batches are always routed to the coldest running machine to maximize thermal headroom. If no machines are currently running, the batch is rerouted and a system-level warning alert is generated.

## Audit

Every state transition, fault injection, and fault resolution is persisted to maintain a complete history of the machine's lifecycle. Alerts cannot be deleted, only acknowledged, ensuring the alert history remains an append-only log.

## Reticles

The contamination level of a reticle increases monotonically up to a maximum level. A reticle is only usable if its contamination level is below the replacement threshold (0.85) and its usage count is under the maximum limit (5000).

## Thermal Convergence

When a machine is idle and has no active thermal faults, it converges monotonically towards the ambient baseline temperature (20.0°C) without overshooting it.

## Health Scoring

The machine health score has a single canonical definition. Both the health and comparison endpoints report the exact same score for a machine at any given time.
