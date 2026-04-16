namespace LithoTwinAPI.Domain;

/// <summary>
/// Named physical and operational constants used throughout the system.
/// Centralizing these eliminates magic numbers and makes the system's
/// assumptions explicit and auditable.
/// </summary>
public static class SystemConstants
{
    // --- Overlay budget ---
    /// <summary>Maximum acceptable overlay error (nm). Exposures exceeding this fail quality checks.</summary>
    public const double OverlaySpecLimitNm = 1.5;

    /// <summary>Thermal expansion coefficient for overlay calculation (nm/°C above baseline).</summary>
    public const double ThermalExpansionCoefficientNmPerC = 0.08;

    /// <summary>Focus penalty coefficient for overlay calculation (nm per nm of focus offset).</summary>
    public const double FocusPenaltyCoefficientNmPerNm = 0.003;

    // --- Thermal management ---
    /// <summary>Temperature hysteresis margin (°C). Machine reactivates only when this far below max temp.</summary>
    public const double CoolingHysteresisMarginC = 2.0;

    /// <summary>Base temperature increase per exposure (°C). Each shot deposits energy into the stage.</summary>
    public const double ExposureHeatContributionBaseC = 0.02;

    /// <summary>Maximum random additional heat per exposure (°C).</summary>
    public const double ExposureHeatContributionVarianceC = 0.03;

    // --- Maintenance ---
    /// <summary>Nominal hours between scheduled maintenance cycles.</summary>
    public const double MaintenanceIntervalHours = 2000.0;

    /// <summary>Hours remaining before maintenance is classified as imminent.</summary>
    public const double MaintenanceImminentThresholdHours = 100.0;

    /// <summary>Hours remaining before maintenance is classified as upcoming.</summary>
    public const double MaintenanceUpcomingThresholdHours = 500.0;

    public const double OverlayDegradationThresholdNm = 1.0;

    /// <summary>Maximum overlay noise amplitude added per exposure (nm).</summary>
    public const double OverlayNoiseAmplitudeNm = 0.4;

    /// <summary>Multiplier for overlay degradation based on throughput loss.</summary>
    public const double OverlayDegradationMaxMultiplier = 0.5;

    // --- Telemetry ---
    /// <summary>Minimum plausible sensor reading (°C). Below this is treated as sensor garbage.</summary>
    public const double SensorMinimumPlausibleC = -10.0;

    /// <summary>Maximum plausible sensor reading (°C). Above this is treated as sensor garbage.</summary>
    public const double SensorMaximumPlausibleC = 80.0;

    /// <summary>Baseline ambient temperature (°C) used in overlay thermal factor calculation.</summary>
    public const double AmbientBaselineC = 20.0;

    // --- Throughput ---
    /// <summary>Throughput multiplier when machine is calibrating (reduced capacity).</summary>
    public const double CalibratingThroughputFactor = 0.5;

    // --- Fault degradation ---
    /// <summary>Throughput degradation factor applied per active LaserDegradation fault.</summary>
    public const double LaserDegradationThroughputPenalty = 0.3;

    // --- Health scoring ---
    public const double HealthWeightTemperature = 0.5;
    public const double HealthWeightUptime = 0.2;
    public const double HealthWeightState = 0.3;
    public const double HealthPenaltyPerActiveFault = 15.0;

    /// <summary>Temperature spike (°C) injected per tick when ThermalOverload is active.</summary>
    public const double ThermalOverloadDriftSpikeC = 0.5;

    /// <summary>Noise amplitude (°C) added to sensor readings when SensorFailure is active.</summary>
    public const double SensorFailureNoiseAmplitudeC = 2.0;

    // --- Ambient convergence ---
    /// <summary>
    /// Dimensionless convergence coefficient per simulation tick towards ambient baseline.
    /// Thermal drift of an Idle machine equals (AmbientBaselineC − temperature) × this rate.
    /// </summary>
    public const double AmbientConvergenceRatePerTick = 0.01;

    // --- Reticles ---
    /// <summary>Maximum contamination level before a reticle must be replaced (0.0 - 1.0).</summary>
    public const double ReticleContaminationReplacementThreshold = 0.85;

    /// <summary>Maximum theoretical contamination level cap for reticles (1.0 = 100%).</summary>
    public const double MaxContaminationLevel = 1.0;

    /// <summary>Base contamination increment per inspection cycle (0–1).</summary>
    public const double ReticleContaminationPerInspection = 0.02;

    /// <summary>Maximum random variance component added per inspection cycle (0–1).</summary>
    public const double ReticleContaminationInspectionVariance = 0.03;
}
