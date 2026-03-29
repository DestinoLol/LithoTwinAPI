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

    [Fact]
    public void idle_state_cools_when_temperature_above_ambient()
    {
        var faults = Array.Empty<FaultType>();
        // Machine at 25°C, ambient baseline is 20°C -> drift should be negative (cooling)
        double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, currentTemp: 25.0);
        Assert.True(drift < 0);
    }

    [Fact]
    public void idle_state_warms_when_temperature_below_ambient()
    {
        var faults = Array.Empty<FaultType>();
        // Machine at 15°C, ambient baseline is 20°C -> drift should be positive (warming)
        double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, currentTemp: 15.0);
        Assert.True(drift > 0);
    }

    [Fact]
    public void is_overheat_condition_detects_exceeded_threshold_on_running_machine()
    {
        var machine = new Machine
        {
            Id = "NXE-3400B",
            State = MachineLifecycleState.Running,
            CurrentTemperature = 25.5,
            MaxOperatingTemp = 24.0
        };
        var faults = Array.Empty<FaultType>();

        bool isOverheat = SimulationEngine.IsOverheatCondition(machine, faults);
        Assert.True(isOverheat);
    }

    [Fact]
    public void is_overheat_condition_ignores_if_already_faulted_with_thermal_overload()
    {
        var machine = new Machine
        {
            Id = "NXE-3400B",
            State = MachineLifecycleState.Running,
            CurrentTemperature = 25.5,
            MaxOperatingTemp = 24.0
        };
        var faults = new[] { FaultType.ThermalOverload };

        bool isOverheat = SimulationEngine.IsOverheatCondition(machine, faults);
        Assert.False(isOverheat);
    }
}
