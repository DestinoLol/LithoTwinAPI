using LithoTwinAPI.Models;
using LithoTwinAPI.Models.Responses;
using LithoTwinAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LithoTwinAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReticleController : ControllerBase
{
    private readonly ReticleService _reticleService;

    public ReticleController(ReticleService reticleService)
    {
        _reticleService = reticleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Reticle>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _reticleService.GetAllAsync());

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Reticle), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            return Ok(await _reticleService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Simulates a reticle inspection. Contamination increases with each handling cycle
    /// due to particle deposition from outgassing and EUV photon exposure.
    /// </summary>
    [HttpPost("{id}/inspect")]
    [ProducesResponseType(typeof(ReticleInspectionResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Inspect(string id)
    {
        try
        {
            var reticle = await _reticleService.InspectAsync(id);
            string? warning = !reticle.IsUsable
                ? "Reticle no longer meets usability criteria — schedule replacement"
                : null;
                
            return Ok(new ReticleInspectionResponse(reticle, warning));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
    }
}
