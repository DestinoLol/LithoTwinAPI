namespace LithoTwinAPI.Models;

/// <summary>
/// Time-series telemetry reading recorded from machine sensors or the simulation engine.
/// </summary>
public class TelemetryReading
{
    /// <summary>Unique telemetry sample identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Machine from which this reading was captured.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Recorded sensor temperature in degrees Celsius (°C).</summary>
    public double Temperature { get; set; }

    /// <summary>UTC timestamp when the reading was recorded.</summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
