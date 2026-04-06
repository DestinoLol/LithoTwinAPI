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
            double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Running, faults, 20.0);
            Assert.InRange(drift, 0.05, 0.15);
        }
    }

    [Fact]
    public void calibrating_state_produces_moderate_heat()
    {
        var faults = Array.Empty<FaultType>();
        for (int i = 0; i < 20; i++)
        {
            double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Calibrating, faults, 20.0);
            Assert.InRange(drift, 0.02, 0.06);
        }
    }

    [Fact]
    public void maintenance_state_produces_zero_drift()
    {
        var faults = Array.Empty<FaultType>();
        double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Maintenance, faults, 20.0);
        Assert.Equal(0, drift);
    }

    [Fact]
    public void thermal_overload_fault_injects_spike_in_drift()
    {
        var faults = new[] { FaultType.ThermalOverload };
        double runningWithFault = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Running, faults, 20.0);
        Assert.True(runningWithFault > SystemConstants.ThermalOverloadDriftSpikeC);
    }

    [Fact]
    public void idle_state_cools_when_temperature_above_ambient()
    {
        var faults = Array.Empty<FaultType>();
        // Machine at 25°C, ambient baseline is 20°C -> drift should be negative (cooling)
        double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, 25.0);
        Assert.True(drift < 0);
    }

    [Fact]
    public void idle_state_warms_when_temperature_below_ambient()
    {
        var faults = Array.Empty<FaultType>();
        // Machine at 15°C, ambient baseline is 20°C -> drift should be positive (warming)
        double drift = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, 15.0);
        Assert.True(drift > 0);
    }

    [Fact]
    public void idle_drift_is_proportional_to_distance_from_ambient()
    {
        var faults = Array.Empty<FaultType>();
        double far = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, 30.0);
        double near = SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, 21.0);
        Assert.True(Math.Abs(far) > Math.Abs(near));
    }

    [Fact]
    public void idle_drift_is_zero_exactly_at_ambient()
    {
        var faults = Array.Empty<FaultType>();
        double drift = SimulationEngine.ComputeThermalDrift(
            MachineLifecycleState.Idle, faults, SystemConstants.AmbientBaselineC);
        Assert.Equal(0, drift, precision: 10);
    }

    [Fact]
    public void idle_machine_settles_at_ambient_and_stays_there()
    {
        var faults = Array.Empty<FaultType>();
        double temp = 30.0;
        for (int i = 0; i < 2000; i++)
            temp += SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, temp);

        Assert.Equal(SystemConstants.AmbientBaselineC, temp, precision: 2);
    }

    [Fact]
    public void idle_machine_below_ambient_warms_up_to_ambient()
    {
        var faults = Array.Empty<FaultType>();
        double temp = 12.0;
        for (int i = 0; i < 2000; i++)
            temp += SimulationEngine.ComputeThermalDrift(MachineLifecycleState.Idle, faults, temp);

        Assert.Equal(SystemConstants.AmbientBaselineC, temp, precision: 2);
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

    [Theory]
    [InlineData(MachineLifecycleState.Running,     25.5, 24.0, true)]
    [InlineData(MachineLifecycleState.Running,     23.0, 24.0, false)]
    [InlineData(MachineLifecycleState.Idle,        25.5, 24.0, false)]
    [InlineData(MachineLifecycleState.Faulted,     25.5, 24.0, false)]
    [InlineData(MachineLifecycleState.Calibrating, 25.5, 24.0, false)]
    [InlineData(MachineLifecycleState.Maintenance, 25.5, 24.0, false)]
    public void overheat_condition_only_triggers_on_running_above_limit(
        MachineLifecycleState state, double current, double max, bool expected)
    {
        var machine = new Machine
        {
            Id = "TEST-01",
            State = state,
            CurrentTemperature = current,
            MaxOperatingTemp = max
        };

        Assert.Equal(expected, SimulationEngine.IsOverheatCondition(machine, Array.Empty<FaultType>()));
    }
}
