namespace LithoTwinAPI.Models.Responses;

public record FactoryStatsResponse(
    MachineStateCounts Machines,
    ProductionTotals Production,
    int ActiveFaultCount,
    System.Collections.Generic.List<AlertSeverityCount> Alerts
);

public record MachineStateCounts(
    int Total,
    int Idle,
    int Calibrating,
    int Running,
    int Faulted,
    int Maintenance
);

public record ProductionTotals(
    int TotalExposures,
    int TotalWafersProcessed,
    double OverallEquipmentEffectiveness
);

public record AlertSeverityCount(string Severity, int Count);