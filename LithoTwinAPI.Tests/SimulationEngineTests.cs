using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using LithoTwinAPI.Simulation;

namespace LithoTwinAPI.Tests;

public class SimulationEngineTests
{
    [Fact]
    public void running_state_accumulates_heat()
    {
        var faults = Array.Empty<FaultType>();
        for (int i = 0; i < 20; i++)
        {
            double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Running, faults);
            Assert.InRange(drift, 0.05, 0.15);
        }
    }

    [Fact]
    public void calibrating_state_produces_moderate_heat()
    {
        var faults = Array.Empty<FaultType>();
        for (int i = 0; i < 20; i++)
        {
            double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Calibrating, faults);
            Assert.InRange(drift, 0.02, 0.06);
        }
    }

    [Fact]
    public void maintenance_state_produces_zero_drift()
    {
        var faults = Array.Empty<FaultType>();
        double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Maintenance, faults);
        Assert.Equal(0, drift);
    }

    [Fact]
    public void thermal_overload_fault_injects_spike_in_drift()
    {
        var faults = new[] { FaultType.ThermalOverload };
        double runningWithFault = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Running, faults);
        Assert.True(runningWithFault > SystemConstants.ThermalOverloadDriftSpikeC);
    }
}
