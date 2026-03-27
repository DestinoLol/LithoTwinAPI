using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using LithoTwinAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Tests;

public class ExposureDiagnosticsTests
{
    private static AppDbContext CreateDb(string name)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        var db = new AppDbContext(opts);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task exposure_failure_populates_failure_reason_and_generates_alert()
    {
        var db = CreateDb("exposure_failure_diag");
        var svc = new ExposureService(db);

        // Intentionally high temperature to trigger thermal overlay expansion exceeding spec limit
        var machine = await db.Machines.FindAsync("NXE-3400B");
        machine!.CurrentTemperature = 45.0;
        await db.SaveChangesAsync();

        var result = await svc.RunExposureAsync(new ExposureRequest
        {
            MachineId = "NXE-3400B",
            LayerId = "M1",
            DoseEnergy = 40.0,
            FocusOffset = 100.0
        });

        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("exceeded spec limit", result.FailureReason);

        var alert = await db.Alerts
            .Where(a => a.MachineId == "NXE-3400B" && a.Severity == AlertSeverity.Warning)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(alert);
        Assert.Contains("Overlay spec exceeded", alert.Message);
    }

    [Fact]
    public async Task exposure_success_has_null_failure_reason()
    {
        var db = CreateDb("exposure_success_diag");
        var svc = new ExposureService(db);

        var machine = await db.Machines.FindAsync("NXE-3400B");
        machine!.CurrentTemperature = 20.0;
        await db.SaveChangesAsync();

        var result = await svc.RunExposureAsync(new ExposureRequest
        {
            MachineId = "NXE-3400B",
            LayerId = "M1",
            DoseEnergy = 35.0,
            FocusOffset = 0.0
        });

        Assert.True(result.Passed);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task exposure_on_non_running_machine_throws_invalid_operation()
    {
        var db = CreateDb("exposure_idle_machine");
        var svc = new ExposureService(db);

        var machine = await db.Machines.FindAsync("TWINSCAN-EXE");
        // TWINSCAN-EXE is seeded in Maintenance state
        Assert.Equal(MachineLifecycleState.Maintenance, machine!.State);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RunExposureAsync(new ExposureRequest
            {
                MachineId = "TWINSCAN-EXE",
                LayerId = "POLY",
                DoseEnergy = 30.0,
                FocusOffset = 0.0
            }));
    }

    [Fact]
    public async Task route_wafer_batch_selects_coldest_running_machine()
    {
        var db = CreateDb("route_coldest");
        var svc = new ExposureService(db);

        // Ensure both machines are running with different temperatures
        var m1 = await db.Machines.FindAsync("NXE-3400B");
        var m2 = await db.Machines.FindAsync("TWINSCAN-EXE");
        m1!.State = MachineLifecycleState.Running;
        m1.CurrentTemperature = 23.5;
        m2!.State = MachineLifecycleState.Running;
        m2.CurrentTemperature = 21.0;
        await db.SaveChangesAsync();

        var batch = await svc.RouteWaferBatchAsync();

        Assert.Equal("TWINSCAN-EXE", batch.AssignedMachineId);
        Assert.Equal(BatchStatus.Processing, batch.Status);
    }
}
