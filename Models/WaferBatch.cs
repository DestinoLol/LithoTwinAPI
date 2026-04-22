namespace LithoTwinAPI.Models;

/// <summary>
/// Execution status of a routed wafer production lot.
/// </summary>
public enum BatchStatus
{
    Pending,
    Processing,
    Rerouted,
    Completed
}

/// <summary>
/// Production lot of semiconductor wafers routed through the EUV lithography cell.
/// </summary>
public class WaferBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AssignedMachineId { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Pending;
    public int WaferCount { get; set; } = 25;
    public string LayerId { get; set; } = "M1";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}