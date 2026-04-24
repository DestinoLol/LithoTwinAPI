using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Services;

/// <summary>
/// Manages reticle lifecycle, contamination inspection, and usability degradation.
/// </summary>
public class ReticleService
{
    private readonly AppDbContext _db;
    

    public ReticleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Reticle>> GetAllAsync()
    {
        return await _db.Reticles.ToListAsync();
    }

    public async Task<Reticle> GetByIdAsync(string id)
    {
        return await _db.Reticles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Reticle '{id}' not found");
    }

    /// <summary>
    /// Simulates a reticle inspection. Contamination increases with handling and photon exposure,
    /// capped at MaxContaminationLevel (1.0).
    /// </summary>
    public async Task<Reticle> InspectAsync(string id)
    {
        var reticle = await GetByIdAsync(id);

        double increment = SystemConstants.ReticleContaminationPerInspection
            + Random.Shared.NextDouble() * SystemConstants.ReticleContaminationInspectionVariance;

        reticle.ContaminationLevel = Math.Round(
            Math.Min(SystemConstants.MaxContaminationLevel, reticle.ContaminationLevel + increment), 3);
        reticle.UsageCount++;

        await _db.SaveChangesAsync();
        return reticle;
    }
}
