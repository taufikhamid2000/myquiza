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

    [HttpPost("api/v1/users/{userId:guid}/achievements")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<AchievementDto>> Award(Guid userId, AwardAchievementDto body)
    {
        var now = DateTime.UtcNow;
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AchievementType = body.AchievementType,
            Title = body.Title,
            Description = body.Description,
            Icon = body.Icon,
            EarnedAt = now,
            Progress = body.Progress,
            MaxProgress = body.MaxProgress,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Achievements.Add(achievement);
        await db.SaveChangesAsync();

        return new AchievementDto(achievement.Id, achievement.AchievementType, achievement.Title,
            achievement.Description, achievement.Icon, achievement.EarnedAt, achievement.Progress, achievement.MaxProgress);
    }
}
