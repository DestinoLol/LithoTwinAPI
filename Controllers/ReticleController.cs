using LithoTwinAPI.Models;
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
    public async Task<IActionResult> GetAll()
        => Ok(await _reticleService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            return Ok(await _reticleService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Simulates a reticle inspection. Contamination increases with each handling cycle
    /// due to particle deposition from outgassing and EUV photon exposure.
    /// </summary>
    [HttpPost("{id}/inspect")]
    public async Task<IActionResult> Inspect(string id)
    {
        try
        {
            return Ok(await _reticleService.InspectAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
