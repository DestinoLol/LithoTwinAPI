namespace LithoTwinAPI.Models.Responses;

public record HealthScoreResponse(
    string MachineId,
    double OverallScore,
    string Comment,
    int ActiveFaultCount,
    double ThroughputFactor,
    HealthBreakdown Breakdown
);

public record HealthBreakdown(
    HealthComponent Temperature,
    HealthComponent Uptime,
    HealthComponent State
);

public record HealthComponent(
    double Score,
    double Weight,
    string Detail
);
