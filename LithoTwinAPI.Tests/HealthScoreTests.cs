using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using LithoTwinAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Tests;

public class HealthScoreTests
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
    public async Task health_score_survives_zero_max_operating_temp()
    {
        var db = CreateDb("health_zero_max_temp");
        var machine = await db.Machines.FindAsync("NXE-3400B");
        machine!.MaxOperatingTemp = 0;
        machine.CurrentTemperature = 0;
        await db.SaveChangesAsync();

        var svc = new MachineLifecycleService(db);
        var health = await svc.ComputeHealthScoreAsync("NXE-3400B");
        var json = System.Text.Json.JsonSerializer.Serialize(health);

        Assert.NotNull(health);
        Assert.DoesNotContain("NaN", json);
    }

    [Fact]
    public async Task health_score_degrades_with_active_faults()
    {
        var db = CreateDb("health_fault_degrades");
        var faultSvc = new FaultService(db);
        var svc = new MachineLifecycleService(db);

        var initialJson = System.Text.Json.JsonSerializer.Serialize(
            await svc.ComputeHealthScoreAsync("NXE-3400B"));
        using var docInitial = System.Text.Json.JsonDocument.Parse(initialJson);
        double initialScore = docInitial.RootElement.GetProperty("overallScore").GetDouble();

        await faultSvc.InjectFaultAsync("NXE-3400B", FaultType.LaserDegradation, "laser power low");

        var degradedJson = System.Text.Json.JsonSerializer.Serialize(
            await svc.ComputeHealthScoreAsync("NXE-3400B"));
        using var docDegraded = System.Text.Json.JsonDocument.Parse(degradedJson);
        double degradedScore = docDegraded.RootElement.GetProperty("overallScore").GetDouble();

        Assert.True(degradedScore < initialScore);
    }

    [Fact]
    public async Task health_score_reports_critical_for_faulted_machine()
    {
        var db = CreateDb("health_critical_status");
        var faultSvc = new FaultService(db);
        var svc = new MachineLifecycleService(db);

        await faultSvc.InjectFaultAsync("NXE-3400B", FaultType.ThermalOverload, "cooling pump failure");

        var json = System.Text.Json.JsonSerializer.Serialize(
            await svc.ComputeHealthScoreAsync("NXE-3400B"));
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        string comment = doc.RootElement.GetProperty("comment").GetString()!;

        Assert.StartsWith("critical", comment);
    }
}
