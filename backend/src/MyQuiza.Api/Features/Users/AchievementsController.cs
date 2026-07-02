using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Features.Users;

[ApiController]
public class AchievementsController(AppDbContext db) : ControllerBase
{
    [HttpGet("api/v1/users/{userId:guid}/achievements")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<IEnumerable<AchievementDto>>> List(Guid userId)
    {
        var items = await db.Achievements
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.EarnedAt)
            .Select(a => new AchievementDto(a.Id, a.AchievementType, a.Title, a.Description, a.Icon, a.EarnedAt, a.Progress, a.MaxProgress))
            .ToListAsync();
        return items;
    }

    /// <summary>
    /// Awards an achievement. Pass achievementId to pull title/description/icon from the
    /// catalog (snapshotted at award time — later catalog edits won't retroactively change
    /// past awards). Omit achievementId and supply the freeform fields for a one-off award.
    /// </summary>
    [HttpPost("api/v1/users/{userId:guid}/achievements")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<AchievementDto>> Award(Guid userId, AwardAchievementDto body)
    {
        string achievementType, title, description, icon;
        int? maxProgress = body.MaxProgress;

        if (body.AchievementId is not null)
        {
            var catalogEntry = await db.AchievementCatalog.FirstOrDefaultAsync(a => a.Id == body.AchievementId);
            if (catalogEntry is null) return BadRequest("achievementId does not match any catalog entry.");

            achievementType = catalogEntry.AchievementType;
            title = catalogEntry.Title;
            description = catalogEntry.Description;
            icon = catalogEntry.Icon;
            maxProgress ??= catalogEntry.MaxProgress;
        }
        else
        {
            if (body.AchievementType is null || body.Title is null || body.Description is null || body.Icon is null)
                return BadRequest("achievementType, title, description, and icon are required when achievementId is omitted.");

            achievementType = body.AchievementType;
            title = body.Title;
            description = body.Description;
            icon = body.Icon;
        }

        var now = DateTime.UtcNow;
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementId = body.AchievementId,
            AchievementType = achievementType,
            Title = title,
            Description = description,
            Icon = icon,
            EarnedAt = now,
            Progress = body.Progress,
            MaxProgress = maxProgress,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Achievements.Add(achievement);
        await db.SaveChangesAsync();

        return new AchievementDto(achievement.Id, achievement.AchievementType, achievement.Title,
            achievement.Description, achievement.Icon, achievement.EarnedAt, achievement.Progress, achievement.MaxProgress);
    }
}
