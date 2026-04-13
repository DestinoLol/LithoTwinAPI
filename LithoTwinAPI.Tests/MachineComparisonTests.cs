using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using LithoTwinAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Tests;

public class MachineComparisonTests
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
    public async Task compare_all_machines_returns_recommendation()
    {
        var db = CreateDb("compare_all_machines");
        var svc = new MachineLifecycleService(db);

        var ids = new List<string> { "NXE-3400B", "NXE-3600D", "TWINSCAN-EXE" };
        var comparison = await svc.CompareMachinesAsync(ids);

        Assert.NotNull(comparison);
        Assert.NotEmpty(comparison.Machines);
        Assert.NotNull(comparison.RecommendedMachineId);
        Assert.False(string.IsNullOrWhiteSpace(comparison.RecommendationReason));
    }

    [Fact]
    public async Task compare_filtered_machines_respects_filter()
    {
        var db = CreateDb("compare_filtered");
        var svc = new MachineLifecycleService(db);

        var filter = new List<string> { "NXE-3400B" };
        var comparison = await svc.CompareMachinesAsync(filter);

        Assert.Single(comparison.Machines);
        Assert.Equal("NXE-3400B", comparison.Machines[0].MachineId);
        Assert.Equal("NXE-3400B", comparison.RecommendedMachineId);
    }

    [Fact]
    public async Task compare_when_all_faulted_returns_no_recommendation()
    {
        var db = CreateDb("compare_all_faulted");
        var faultSvc = new FaultService(db);
        var svc = new MachineLifecycleService(db);

        var machines = await db.Machines.ToListAsync();
        foreach (var m in machines)
        {
            await faultSvc.InjectFaultAsync(m.Id, FaultType.ThermalOverload, "Forced fault");
        }

        var ids = machines.Select(m => m.Id).ToList();
        var comparison = await svc.CompareMachinesAsync(ids);

        Assert.Null(comparison.RecommendedMachineId);
        Assert.Contains("No machines are currently eligible", comparison.RecommendationReason);
    }

    [Fact]
    public async Task compare_nonexistent_machine_throws_key_not_found()
    {
        var db = CreateDb("compare_not_found");
        var svc = new MachineLifecycleService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.CompareMachinesAsync(new List<string> { "NONEXISTENT-MACHINE" }));
    }

    [Fact]
    public async Task compare_partially_nonexistent_machines_throws_key_not_found()
    {
        var db = CreateDb("compare_partial_not_found");
        var svc = new MachineLifecycleService(db);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.CompareMachinesAsync(new List<string> { "NXE-3400B", "GHOST-MACHINE" }));

        Assert.Contains("GHOST-MACHINE", ex.Message);
    }

    [Fact]
    public async Task health_and_compare_report_identical_score_for_machine()
    {
        var db = CreateDb("health_and_compare_consistency");
        var svc = new MachineLifecycleService(db);

        // Test across all seeded machines (Running, Maintenance, etc.)
        var machines = await db.Machines.ToListAsync();
        var ids = machines.Select(m => m.Id).ToList();
        var comparison = await svc.CompareMachinesAsync(ids);

        foreach (var m in machines)
        {
            var healthResult = await svc.ComputeHealthScoreAsync(m.Id);
            double healthScore = healthResult.OverallScore;

            var comparisonEntry = comparison.Machines.First(entry => entry.MachineId == m.Id);

            Assert.Equal(healthScore, comparisonEntry.HealthScore);
        }
    }
}
