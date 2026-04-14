using LithoTwinAPI.Domain;
using LithoTwinAPI.Services;
using LithoTwinAPI.Models;
using LithoTwinAPI.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LithoTwinAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FactoryController : ControllerBase
{
    private readonly MachineLifecycleService _lifecycle;
    private readonly FaultService _faults;
    private readonly TelemetryService _telemetry;
    private readonly ExposureService _exposure;
    private readonly AlertService _alerts;

    public FactoryController(
        MachineLifecycleService lifecycle,
        FaultService faults,
        TelemetryService telemetry,
        ExposureService exposure,
        AlertService alerts)
    {
        _lifecycle = lifecycle;
        _faults = faults;
        _telemetry = telemetry;
        _exposure = exposure;
        _alerts = alerts;
    }

    // ---- telemetry ----

    [HttpPost("telemetry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostTelemetry(
        [FromQuery] string machineId, [FromQuery] double temperature)
    {
        try
        {
            await _telemetry.IngestReadingAsync(machineId, temperature);
            return Ok(new SuccessResponse($"Telemetry recorded for {machineId}"));
        }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(new ErrorResponse(ex.Message)); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new ErrorResponse(ex.Message)); }
    }

    [HttpGet("telemetry/{machineId}/history")]
    [ProducesResponseType(typeof(List<TelemetryReading>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTelemetryHistory(string machineId, [FromQuery] int count = 50)
        => Ok(await _telemetry.GetHistoryAsync(machineId, count));

    [HttpGet("telemetry/{machineId}/trend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrend(string machineId)
        => Ok(new { machineId, trend = await _telemetry.ComputeTrendAsync(machineId) });

    [HttpGet("telemetry/{machineId}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(string machineId)
    {
        var csv = await _telemetry.ExportCsvAsync(machineId);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"telemetry_{machineId}.csv");
    }

    // ---- state transitions ----

    [HttpPost("machines/{machineId}/transition")]
    [ProducesResponseType(typeof(StateTransition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransitionState(
        string machineId,
        [FromQuery] MachineLifecycleState targetState,
        [FromQuery] string reason = "Manual transition")
    {
        try
        {
            var transition = await _lifecycle.TransitionStateAsync(machineId, targetState, reason);
            return Ok(transition);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
        catch (InvalidStateTransitionException ex) { return Conflict(new ErrorResponse(ex.Message)); }
    }

    [HttpGet("machines/{machineId}/transitions")]
    [ProducesResponseType(typeof(List<StateTransition>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransitionHistory(string machineId)
        => Ok(await _lifecycle.GetTransitionHistoryAsync(machineId));

    // ---- fault management ----

    [HttpPost("machines/{machineId}/fault")]
    [ProducesResponseType(typeof(MachineFault), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InjectFault(
        string machineId,
        [FromQuery] FaultType faultType,
        [FromQuery] string description = "")
    {
        try
        {
            var fault = await _faults.InjectFaultAsync(machineId, faultType, description);
            return Ok(fault);
        }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
    }

    [HttpPost("machines/{machineId}/resolve-faults")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveFaults(string machineId)
    {
        try
        {
            var resolved = await _faults.ResolveFaultsAsync(machineId);
            return Ok(new { resolvedCount = resolved.Count, faults = resolved });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(new ErrorResponse(ex.Message)); }
    }

    [HttpGet("machines/{machineId}/faults")]
    [ProducesResponseType(typeof(List<MachineFault>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveFaults(string machineId)
        => Ok(await _faults.GetActiveFaultsAsync(machineId));

    // ---- wafer routing ----

    [HttpPost("route-wafer")]
    [ProducesResponseType(typeof(WaferBatch), StatusCodes.Status200OK)]
    public async Task<IActionResult> RouteWafer()
        => Ok(await _exposure.RouteWaferBatchAsync());

    [HttpPost("batches/{id}/complete")]
    [ProducesResponseType(typeof(WaferBatch), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteBatch(Guid id)
    {
        try { return Ok(await _exposure.CompleteBatchAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(new ErrorResponse(ex.Message)); }
    }

    // ---- machines ----

    [HttpGet("system-status")]
    [ProducesResponseType(typeof(List<Machine>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
        => Ok(await _lifecycle.GetAllMachinesAsync());

    [HttpGet("machines/{machineId}/health")]
    [ProducesResponseType(typeof(HealthScoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHealth(string machineId)
    {
        try { return Ok(await _lifecycle.ComputeHealthScoreAsync(machineId)); }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
    }

    [HttpGet("machines/{machineId}/maintenance-prediction")]
    [ProducesResponseType(typeof(MaintenancePredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PredictMaintenance(string machineId)
    {
        try { return Ok(await _lifecycle.PredictMaintenanceAsync(machineId)); }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
    }

    [HttpGet("machines/compare")]
    [ProducesResponseType(typeof(MachineComparison), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareMachines([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return BadRequest(new ErrorResponse("Query parameter 'ids' is required"));

        var machineIds = ids
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (machineIds.Count < 2)
            return BadRequest(new ErrorResponse("Provide at least 2 comma-separated machine IDs"));

        try { return Ok(await _lifecycle.CompareMachinesAsync(machineIds)); }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
    }

    // ---- alerts & stats ----

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(List<Alert>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts()
        => Ok(await _alerts.GetActiveAlertsAsync());

    [HttpPost("alerts/{id}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcknowledgeAlert(Guid id)
    {
        try
        {
            await _alerts.AcknowledgeAsync(id);
            return Ok(new SuccessResponse("Alert acknowledged"));
        }
        catch (KeyNotFoundException ex) { return NotFound(new ErrorResponse(ex.Message)); }
    }

    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
        => Ok(await _alerts.GetFactoryStatsAsync());
}
