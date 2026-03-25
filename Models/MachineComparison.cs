using LithoTwinAPI.Domain;

namespace LithoTwinAPI.Models;

/// <summary>
/// Detailed comparison entry for a single machine within a comparison query.
/// </summary>
public class MachineComparisonEntry
{
    public string MachineId { get; set; } = string.Empty;
    public MachineLifecycleState State { get; set; }
    public double CurrentTemperature { get; set; }
    public double MaxOperatingTemp { get; set; }
    public double ThermalHeadroom => Math.Max(0, MaxOperatingTemp - CurrentTemperature);
    public double UptimeHours { get; set; }
    public int ExposureCount { get; set; }
    public double ThroughputFactor { get; set; }
    public int ActiveFaultCount { get; set; }
    public double HealthScore { get; set; }
    public bool IsEligibleForProduction => State == MachineLifecycleState.Running && ActiveFaultCount == 0;
}

/// <summary>
/// Aggregated machine comparison report with production recommendations.
/// </summary>
public class MachineComparison
{
    public List<MachineComparisonEntry> Machines { get; set; } = new();
    public string? RecommendedMachineId { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
}
