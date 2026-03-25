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

        var comparison = await svc.CompareMachinesAsync();

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

        var comparison = await svc.CompareMachinesAsync();

        Assert.Null(comparison.RecommendedMachineId);
        Assert.Contains("No machines are currently eligible", comparison.RecommendationReason);
    }

    [Fact]
    public async Task compare_nonexistent_machine_throws_invalid_operation()
    {
        var db = CreateDb("compare_not_found");
        var svc = new MachineLifecycleService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CompareMachinesAsync(new List<string> { "NONEXISTENT-MACHINE" }));
    }
}
