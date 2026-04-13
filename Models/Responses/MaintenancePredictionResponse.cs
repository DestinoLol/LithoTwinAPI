namespace LithoTwinAPI.Models.Responses;

public record MaintenancePredictionResponse(
    string MachineId,
    double HoursUntilMaintenance,
    string Urgency,
    double? AverageOverlayNm,
    bool IsOverlayDegrading,
    int ActiveFaultCount
);
