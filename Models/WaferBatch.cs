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
    /// <summary>Unique batch identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Assigned machine processing this batch.</summary>
    public string AssignedMachineId { get; set; } = string.Empty;

    /// <summary>Current workflow lifecycle status of the batch.</summary>
    public BatchStatus Status { get; set; } = BatchStatus.Pending;

    /// <summary>Number of wafers in this lot (standard FOUP carrier = 25 wafers).</summary>
    public int WaferCount { get; set; } = 25;

    /// <summary>Layer to expose on all wafers in this batch.</summary>
    public string LayerId { get; set; } = "M1";

    /// <summary>UTC timestamp when the batch was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}