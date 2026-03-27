using LithoTwinAPI.Data;
using LithoTwinAPI.Domain;
using LithoTwinAPI.Models;
using LithoTwinAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LithoTwinAPI.Tests;

public class ReticleTests
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
    public async Task reticle_inspect_increases_contamination_and_usage_count()
    {
        var db = CreateDb("reticle_inspect_basic");
        var svc = new ReticleService(db);

        var initial = await svc.GetByIdAsync("RET-001");
        double initialContamination = initial.ContaminationLevel;
        int initialUsage = initial.UsageCount;

        await svc.InspectAsync("RET-001");

        var updated = await svc.GetByIdAsync("RET-001");
        Assert.True(updated.ContaminationLevel > initialContamination);
        Assert.Equal(initialUsage + 1, updated.UsageCount);
    }

    [Fact]
    public async Task reticle_contamination_is_capped_at_max_level()
    {
        var db = CreateDb("reticle_contamination_cap");
        var svc = new ReticleService(db);

        var reticle = await db.Reticles.FindAsync("RET-001");
        reticle!.ContaminationLevel = 0.99;
        await db.SaveChangesAsync();

        await svc.InspectAsync("RET-001");

        var updated = await svc.GetByIdAsync("RET-001");
        Assert.Equal(SystemConstants.MaxContaminationLevel, updated.ContaminationLevel);
    }

    [Fact]
    public async Task reticle_is_unusable_when_contamination_exceeds_threshold()
    {
        var db = CreateDb("reticle_unusable_contamination");
        var reticle = await db.Reticles.FindAsync("RET-001");
        reticle!.ContaminationLevel = SystemConstants.ReticleContaminationReplacementThreshold + 0.05;
        await db.SaveChangesAsync();

        Assert.False(reticle.IsUsable);
    }

    [Fact]
    public async Task reticle_is_unusable_when_usage_exceeds_max()
    {
        var db = CreateDb("reticle_unusable_max_usage");
        var reticle = await db.Reticles.FindAsync("RET-001");
        reticle!.UsageCount = reticle.MaxUsages + 1;
        await db.SaveChangesAsync();

        Assert.False(reticle.IsUsable);
    }

    [Fact]
    public async Task reticle_get_nonexistent_throws_key_not_found()
    {
        var db = CreateDb("reticle_not_found");
        var svc = new ReticleService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.GetByIdAsync("NON-EXISTENT-RETICLE"));
    }
}
