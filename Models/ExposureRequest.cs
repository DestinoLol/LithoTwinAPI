namespace LithoTwinAPI.Models;

/// <summary>
/// Parameters for simulating a single EUV exposure shot on a wafer layer.
/// </summary>
public class ExposureRequest
{
    /// <summary>Target machine identifier (must be in Running state).</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>EUV dose energy (mJ/cm²), nominal ~30.0 mJ/cm².</summary>
    public double DoseEnergy { get; set; } = 30.0;

    /// <summary>Wafer stage focus offset in nanometers (0 = nominal focus plane).</summary>
    public double FocusOffset { get; set; }

    /// <summary>Semiconductor layer identifier being patterned (e.g., M1, VIA1, POLY).</summary>
    public string LayerId { get; set; } = "M1";
}
