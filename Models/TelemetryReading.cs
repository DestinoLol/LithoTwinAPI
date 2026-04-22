namespace LithoTwinAPI.Models;

/// <summary>
/// Time-series telemetry reading recorded from machine sensors or the simulation engine.
/// </summary>
public class TelemetryReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MachineId { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
