using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using LithoTwinAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Tests;

public class BatchAndExportTests
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
    public async Task export_csv_formats_headers_and_rows_correctly()
    {
        var db = CreateDb("export_csv_format");
        var faultSvc = new FaultService(db);
        var telemetrySvc = new TelemetryService(db, faultSvc);

        await telemetrySvc.IngestReadingAsync("NXE-3400B", 21.5);
        await telemetrySvc.IngestReadingAsync("NXE-3400B", 22.0);

        var csv = await telemetrySvc.ExportCsvAsync("NXE-3400B");

        Assert.Contains("timestamp,machine_id,temperature_c", csv);
        Assert.Contains("NXE-3400B", csv);
        Assert.Contains("21.50", csv);
        Assert.Contains("22.00", csv);
    }

    [Fact]
    public async Task complete_batch_marks_batch_completed_and_increments_total_wafers()
    {
        var db = CreateDb("complete_batch_success");
        var exposureSvc = new ExposureService(db);

        var batch = await exposureSvc.RouteWaferBatchAsync();
        Assert.Equal(BatchStatus.Processing, batch.Status);

        var machineBefore = await db.Machines.FindAsync(batch.AssignedMachineId);
        int wafersBefore = machineBefore!.TotalWafersProcessed;

        var completed = await exposureSvc.CompleteBatchAsync(batch.Id);
        Assert.Equal(BatchStatus.Completed, completed.Status);

        var machineAfter = await db.Machines.FindAsync(batch.AssignedMachineId);
        Assert.Equal(wafersBefore + batch.WaferCount, machineAfter!.TotalWafersProcessed);
    }

    [Fact]
    public async Task complete_batch_nonexistent_throws_key_not_found()
    {
        var db = CreateDb("complete_batch_not_found");
        var exposureSvc = new ExposureService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => exposureSvc.CompleteBatchAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task complete_batch_already_completed_throws_invalid_operation()
    {
        var db = CreateDb("complete_batch_already_done");
        var exposureSvc = new ExposureService(db);

        var batch = await exposureSvc.RouteWaferBatchAsync();
        await exposureSvc.CompleteBatchAsync(batch.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => exposureSvc.CompleteBatchAsync(batch.Id));
    }

    [Fact]
    public async Task get_factory_stats_aggregates_machine_and_alert_metrics()
    {
        var db = CreateDb("factory_stats_agg");
        var alertSvc = new AlertService(db);

        db.Alerts.Add(new Alert
        {
            MachineId = "NXE-3400B",
            Severity = AlertSeverity.Warning,
            Message = "Test warning"
        });
        await db.SaveChangesAsync();

        var stats = await alertSvc.GetFactoryStatsAsync();
        var json = System.Text.Json.JsonSerializer.Serialize(stats);

        Assert.Contains("\"total\":3", json);
        Assert.Contains("production", json);
        Assert.Contains("alerts", json);
    }
}
