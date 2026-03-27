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
    private static readonly Random _rng = new();

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
    public async Task<object> InspectAsync(string id)
    {
        var ret = await GetByIdAsync(id);

        double increase = 0.02 + _rng.NextDouble() * 0.03;
        ret.ContaminationLevel = Math.Min(
            SystemConstants.MaxContaminationLevel,
            Math.Round(ret.ContaminationLevel + increase, 3));
        ret.UsageCount++;

        await _db.SaveChangesAsync();

        return new
        {
            reticle = ret,
            warning = !ret.IsUsable
                ? "Reticle no longer meets usability criteria — schedule replacement"
                : (string?)null
        };
    }
}
