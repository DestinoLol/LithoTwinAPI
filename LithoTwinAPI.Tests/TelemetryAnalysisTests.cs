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

    [Fact]
    public async Task trend_detects_rising_temperature()
    {
        var db = CreateDb("trend_rising");
        var faultSvc = new FaultService(db);
        var telemetrySvc = new TelemetryService(db, faultSvc);

        var now = DateTime.UtcNow;
        for (int i = 0; i < 6; i++)
        {
            db.TelemetryReadings.Add(new TelemetryReading
            {
                MachineId = "NXE-3400B",
                Temperature = 20.0 + (i * 0.5), // 20.0, 20.5, 21.0, 21.5, 22.0, 22.5
                RecordedAt = now.AddSeconds(i * 10)
            });
        }
        await db.SaveChangesAsync();

        var trend = await telemetrySvc.ComputeTrendAsync("NXE-3400B");
        Assert.Equal("rising", trend);
    }

    [Fact]
    public async Task trend_detects_falling_temperature()
    {
        var db = CreateDb("trend_falling");
        var faultSvc = new FaultService(db);
        var telemetrySvc = new TelemetryService(db, faultSvc);

        var now = DateTime.UtcNow;
        for (int i = 0; i < 6; i++)
        {
            db.TelemetryReadings.Add(new TelemetryReading
            {
                MachineId = "NXE-3400B",
                Temperature = 25.0 - (i * 0.5),
                RecordedAt = now.AddSeconds(i * 10)
            });
        }
        await db.SaveChangesAsync();

        var trend = await telemetrySvc.ComputeTrendAsync("NXE-3400B");
        Assert.Equal("falling", trend);
    }

    [Fact]
    public async Task trend_detects_stable_temperature()
    {
        var db = CreateDb("trend_stable");
        var faultSvc = new FaultService(db);
        var telemetrySvc = new TelemetryService(db, faultSvc);

        var now = DateTime.UtcNow;
        for (int i = 0; i < 6; i++)
        {
            db.TelemetryReadings.Add(new TelemetryReading
            {
                MachineId = "NXE-3400B",
                Temperature = 22.0 + (i % 2 == 0 ? 0.05 : -0.05),
                RecordedAt = now.AddSeconds(i * 10)
            });
        }
        await db.SaveChangesAsync();

        var trend = await telemetrySvc.ComputeTrendAsync("NXE-3400B");
        Assert.Equal("stable", trend);
    }

    [Fact]
    public async Task trend_returns_insufficient_data_when_less_than_4_readings()
    {
        var db = CreateDb("trend_insufficient");
        var faultSvc = new FaultService(db);
        var telemetrySvc = new TelemetryService(db, faultSvc);

        var now = DateTime.UtcNow;
        db.TelemetryReadings.Add(new TelemetryReading
        {
            MachineId = "NXE-3400B",
            Temperature = 22.0,
            RecordedAt = now
        });
        db.TelemetryReadings.Add(new TelemetryReading
        {
            MachineId = "NXE-3400B",
            Temperature = 23.0,
            RecordedAt = now.AddSeconds(10)
        });
        await db.SaveChangesAsync();

        var trend = await telemetrySvc.ComputeTrendAsync("NXE-3400B");
        Assert.Equal("insufficient_data", trend);
    }
}
