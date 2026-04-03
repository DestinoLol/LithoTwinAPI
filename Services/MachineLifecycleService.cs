using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Services;

/// <summary>
/// Manages machine lifecycle state transitions through the FSM.
/// All state changes are validated, audited, and produce explicit domain errors on violation.
/// </summary>
public class MachineLifecycleService
{
    private readonly AppDbContext _db;

    public MachineLifecycleService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Performs a validated state transition through the FSM.
    /// Every transition is recorded in the audit log.
    /// Throws <see cref="InvalidStateTransitionException"/> if the transition is forbidden.
    /// </summary>
    public async Task<StateTransition> TransitionStateAsync(
        string machineId, MachineLifecycleState targetState, string reason)
    {
        var machine = await FindMachineOrThrowAsync(machineId);
        var fsm = new MachineStateMachine(machine.State);

        var transition = fsm.TransitionTo(targetState, machineId, reason);

        machine.State = fsm.CurrentState;
        machine.LastUpdated = DateTime.UtcNow;

        _db.StateTransitions.Add(transition);
        await _db.SaveChangesAsync();

        return transition;
    }

    public async Task<List<StateTransition>> GetTransitionHistoryAsync(string machineId)
    {
        return await _db.StateTransitions
            .Where(t => t.MachineId == machineId)
            .OrderByDescending(t => t.TransitionedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<List<Machine>> GetAllMachinesAsync()
        => await _db.Machines.ToListAsync();

    /// <summary>
    /// Computes machine health as a weighted score of temperature, uptime, and lifecycle state.
    /// Active faults degrade the score proportionally.
    /// </summary>
    public async Task<object> ComputeHealthScoreAsync(string machineId)
    {
        var machine = await FindMachineOrThrowAsync(machineId);
        var activeFaultCount = await _db.MachineFaults
            .Where(f => f.MachineId == machineId && f.ResolvedAt == null)
            .CountAsync();

        double overall = ComputeOverallHealthScore(machine, activeFaultCount);
        double tempScore = ComputeTemperatureScore(machine);
        double uptimeScore = ComputeUptimeScore(machine);
        double stateScore = machine.State switch
        {
            MachineLifecycleState.Running => 100,
            MachineLifecycleState.Calibrating => 70,
            MachineLifecycleState.Idle => 50,
            MachineLifecycleState.Maintenance => 10,
            MachineLifecycleState.Faulted => 0,
            _ => 0
        };

        string comment = overall switch
        {
            >= 80 => "healthy — nominal operating conditions",
            >= 60 => "degraded — monitor closely",
            >= 40 => "needs attention — schedule maintenance",
            _ => "critical — take offline"
        };

        return new
        {
            machineId,
            overallScore = Math.Round(overall, 1),
            comment,
            activeFaultCount,
            throughputFactor = machine.ThroughputFactor,
            breakdown = new
            {
                temperature = new { score = Math.Round(tempScore, 1), weight = 0.5,
                    detail = $"{machine.CurrentTemperature:F1}°C / {machine.MaxOperatingTemp:F1}°C" },
                uptime = new { score = Math.Round(uptimeScore, 1), weight = 0.2,
                    detail = $"{machine.UptimeHours:F0}h" },
                state = new { score = stateScore, weight = 0.3,
                    detail = machine.State.ToString() }
            }
        };
    }

    /// <summary>
    /// Predicts maintenance urgency based on uptime cycles and overlay drift monitoring.
    /// </summary>
    public async Task<object> PredictMaintenanceAsync(string machineId)
    {
        var machine = await FindMachineOrThrowAsync(machineId);

        double hoursLeft = Math.Max(0,
            SystemConstants.MaintenanceIntervalHours -
            (machine.UptimeHours % SystemConstants.MaintenanceIntervalHours));

        string urgency;
        if (hoursLeft < SystemConstants.MaintenanceImminentThresholdHours) urgency = "imminent";
        else if (hoursLeft < SystemConstants.MaintenanceUpcomingThresholdHours) urgency = "upcoming";
        else urgency = "not_due";

        var recentExposures = await _db.ExposureResults
            .Where(e => e.MachineId == machineId)
            .OrderByDescending(e => e.ExposedAt)
            .Take(20)
            .ToListAsync();

        double? avgOverlay = null;
        bool overlayDegrading = false;
        if (recentExposures.Count >= 5)
        {
            avgOverlay = recentExposures.Average(e =>
                Math.Sqrt(e.OverlayErrorX * e.OverlayErrorX + e.OverlayErrorY * e.OverlayErrorY));
            if (avgOverlay > SystemConstants.OverlayDegradationThresholdNm)
                overlayDegrading = true;
        }

        var activeFaultCount = await _db.MachineFaults
            .Where(f => f.MachineId == machineId && f.ResolvedAt == null)
            .CountAsync();

        return new
        {
            machineId,
            estimatedHoursUntilMaintenance = Math.Round(hoursLeft, 0),
            urgency,
            overlayDegrading,
            activeFaultCount,
            avgOverlayNm = avgOverlay.HasValue ? Math.Round(avgOverlay.Value, 3) : (double?)null,
            note = machine.State == MachineLifecycleState.Maintenance
                ? "currently in maintenance"
                : machine.State == MachineLifecycleState.Faulted
                    ? "faulted — maintenance required before resuming production"
                    : overlayDegrading
                        ? "overlay trending up — consider scheduling maintenance"
                        : null
        };
    }

    /// <summary>
    /// Compares machines by health score, thermal headroom, active faults, and production eligibility.
    /// Returns an optimal production recommendation.
    /// </summary>
    public async Task<MachineComparison> CompareMachinesAsync(List<string>? machineIds = null)
    {
        var query = _db.Machines.AsQueryable();
        if (machineIds != null && machineIds.Any())
            query = query.Where(m => machineIds.Contains(m.Id));

        var machines = await query.ToListAsync();

        if (machineIds != null && machineIds.Any())
        {
            var missing = machineIds.Except(machines.Select(m => m.Id)).ToList();
            if (missing.Any())
                throw new KeyNotFoundException($"Machine(s) not found: {string.Join(", ", missing)}");
        }

        if (!machines.Any())
            throw new KeyNotFoundException("No machines found matching the specified criteria.");

        var activeFaults = await _db.MachineFaults
            .Where(f => f.ResolvedAt == null)
            .ToListAsync();

        var entries = new List<MachineComparisonEntry>();
        foreach (var m in machines)
        {
            var faultCount = activeFaults.Count(f => f.MachineId == m.Id);
            double overall = ComputeOverallHealthScore(m, faultCount);

            entries.Add(new MachineComparisonEntry
            {
                MachineId = m.Id,
                State = m.State,
                CurrentTemperature = m.CurrentTemperature,
                MaxOperatingTemp = m.MaxOperatingTemp,
                UptimeHours = m.UptimeHours,
                ExposureCount = m.ExposureCount,
                ThroughputFactor = m.ThroughputFactor,
                ActiveFaultCount = faultCount,
                HealthScore = Math.Round(overall, 1)
            });
        }

        var eligible = entries.Where(e => e.IsEligibleForProduction).ToList();
        string? recommendedId = null;
        string recommendationReason;

        if (eligible.Any())
        {
            // Pick highest health score, break ties with thermal headroom
            var best = eligible
                .OrderByDescending(e => e.HealthScore)
                .ThenByDescending(e => e.ThermalHeadroom)
                .First();
            recommendedId = best.MachineId;
            recommendationReason = $"Machine '{best.MachineId}' has the highest health score ({best.HealthScore:F1}) with {best.ThermalHeadroom:F1}°C thermal headroom.";
        }
        else
        {
            recommendationReason = "No machines are currently eligible for production (all faulted, in maintenance, or idle).";
        }

        return new MachineComparison
        {
            Machines = entries.OrderByDescending(e => e.HealthScore).ToList(),
            RecommendedMachineId = recommendedId,
            RecommendationReason = recommendationReason,
            ComparedAt = DateTime.UtcNow
        };
    }

    // ---- scoring helpers ----

    /// <summary>
    /// Health score (0–100) of a machine: weighted average of temperature, uptime, and lifecycle state,
    /// degraded by 15 points per active fault.
    /// Single shared definition used by ComputeHealthScoreAsync and CompareMachinesAsync to ensure
    /// consistent metrics across endpoints.
    /// </summary>
    private static double ComputeOverallHealthScore(Machine machine, int activeFaultCount)
    {
        double stateScore = machine.State switch
        {
            MachineLifecycleState.Running => 100,
            MachineLifecycleState.Calibrating => 70,
            MachineLifecycleState.Idle => 50,
            MachineLifecycleState.Maintenance => 10,
            MachineLifecycleState.Faulted => 0,
            _ => 0
        };

        // Weights: temperature is the dominant constraint for EUV optics stability
        double overall = (ComputeTemperatureScore(machine) * 0.5)
                       + (ComputeUptimeScore(machine) * 0.2)
                       + (stateScore * 0.3);

        return Math.Max(0, overall - (activeFaultCount * 15));
    }

    private static double ComputeTemperatureScore(Machine m)
    {
        // Guard against division by zero if MaxOperatingTemp is not configured (prevents NaN crashing JSON serialization)
        if (m.MaxOperatingTemp <= 0) return 0;

        double ratio = m.CurrentTemperature / m.MaxOperatingTemp;
        if (ratio < 0.7) return 100;
        if (ratio > 1.0) return 0;
        return (1.0 - ratio) / 0.3 * 100;
    }

    private static double ComputeUptimeScore(Machine m)
    {
        if (m.UptimeHours < 500) return 100;
        if (m.UptimeHours > 3000) return 20;
        return 100 - (m.UptimeHours - 500) / 2500 * 80;
    }

    internal async Task<Machine> FindMachineOrThrowAsync(string machineId)
        => await _db.Machines.FindAsync(machineId)
            ?? throw new KeyNotFoundException($"No machine found with id '{machineId}'");
}
