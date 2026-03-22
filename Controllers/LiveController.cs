using LithoTwinAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Controllers;

[ApiController]
[Route("api/live")]
public class LiveController : ControllerBase
{
    private readonly AppDbContext _db;

    public LiveController(AppDbContext db) => _db = db;

    /// <summary>
    /// SSE endpoint — streams new alerts as they arrive.
    /// Uses polling internally; SignalR is intentionally avoided to keep the
    /// dependency surface small for what is a monitoring/diagnostic feature.
    /// </summary>
    [HttpGet("alerts")]
    public async Task StreamAlerts(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        // Disable reverse proxy buffering so events are delivered immediately
        Response.Headers["X-Accel-Buffering"] = "no";

        var lastCheck = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            // AsNoTracking: stream is read-only and long-lived — tracked entities would stay in memory indefinitely
            var newAlerts = await _db.Alerts
                .AsNoTracking()
                .Where(a => a.Timestamp > lastCheck)
                .OrderBy(a => a.Timestamp)
                .ToListAsync(ct);

            foreach (var alert in newAlerts)
            {
                var data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = alert.Id,
                    machineId = alert.MachineId,
                    message = alert.Message,
                    severity = alert.Severity.ToString(),
                    timestamp = alert.Timestamp
                });

                await Response.WriteAsync($"data: {data}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            if (newAlerts.Any())
                lastCheck = newAlerts.Last().Timestamp;

            await Task.Delay(3000, ct);
        }
    }
}
