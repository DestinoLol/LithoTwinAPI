using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using LithoTwinAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Tests;

public class TelemetryAnalysisTests
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
    public async Task telemetry_persists_thermal_overload_drift_spike()
    {
        var db = CreateDb("telemetry_spike_persisted");
        var faultSvc = new FaultService(db);
        var telemetrySvc = new TelemetryService(db, faultSvc);

        // Inject active ThermalOverload fault
        db.MachineFaults.Add(new MachineFault
        {
            MachineId = "NXE-3400B",
            FaultType = FaultType.ThermalOverload,
            Description = "Cooling system degraded",
            OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        double baseTemp = 22.0;
        await telemetrySvc.IngestReadingAsync("NXE-3400B", baseTemp);

        var machine = await db.Machines.FindAsync("NXE-3400B");
        var persistedReading = await db.TelemetryReadings
            .OrderByDescending(r => r.RecordedAt)
            .FirstOrDefaultAsync(r => r.MachineId == "NXE-3400B");

        double expectedTemp = baseTemp + SystemConstants.ThermalOverloadDriftSpikeC;
        Assert.NotNull(machine);
        Assert.NotNull(persistedReading);
        Assert.Equal(expectedTemp, machine.CurrentTemperature, precision: 4);
        Assert.Equal(expectedTemp, persistedReading.Temperature, precision: 4);
    }
}
