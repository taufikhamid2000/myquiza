using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Features.Users;

[ApiController]
public class AchievementCatalogController(AppDbContext db) : ControllerBase
{
    [HttpGet("api/v1/achievements")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AchievementCatalogDto>>> List()
    {
        var items = await db.AchievementCatalog
            .OrderBy(a => a.Title)
            .Select(a => new AchievementCatalogDto(a.Id, a.AchievementType, a.Title, a.Description, a.Icon, a.MaxProgress))
            .ToListAsync();
        return items;
    }

    [HttpPost("api/v1/achievements")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<AchievementCatalogDto>> Create(CreateAchievementCatalogDto body)
    {
        var now = DateTime.UtcNow;
        var entry = new AchievementCatalog
        {
            Id = Guid.NewGuid(),
            AchievementType = body.AchievementType,
            Title = body.Title,
            Description = body.Description,
            Icon = body.Icon,
            MaxProgress = body.MaxProgress,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.AchievementCatalog.Add(entry);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), null,
            new AchievementCatalogDto(entry.Id, entry.AchievementType, entry.Title, entry.Description, entry.Icon, entry.MaxProgress));
    }

    [HttpPatch("api/v1/achievements/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> Update(Guid id, UpdateAchievementCatalogDto body)
    {
        var entry = await db.AchievementCatalog.FirstOrDefaultAsync(a => a.Id == id);
        if (entry is null) return NotFound();

        if (body.AchievementType is not null) entry.AchievementType = body.AchievementType;
        if (body.Title is not null) entry.Title = body.Title;
        if (body.Description is not null) entry.Description = body.Description;
        if (body.Icon is not null) entry.Icon = body.Icon;
        if (body.MaxProgress is not null) entry.MaxProgress = body.MaxProgress;
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("api/v1/achievements/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entry = await db.AchievementCatalog.FirstOrDefaultAsync(a => a.Id == id);
        if (entry is null) return NotFound();

        db.AchievementCatalog.Remove(entry);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
