using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Auth;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;

namespace MyQuiza.Api.Features.Me;

[ApiController]
public class MeController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    [HttpGet("api/v1/me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> Get()
    {
        var userId = currentUser.RequireUserId();

        var profile = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null) return NotFound();

        var role = await db.UserRoles.Where(r => r.UserId == userId)
            .Select(r => r.Role).FirstOrDefaultAsync() ?? "user";

        return new MeDto(profile.Id, profile.DisplayName, profile.AvatarUrl,
            profile.Xp, profile.Level, profile.Streak, profile.SchoolRole, role);
    }

    /// <summary>Self-service profile update. schoolRole is intentionally not editable here — see UpdateMeDto.</summary>
    [HttpPatch("api/v1/me")]
    [Authorize]
    public async Task<IActionResult> Update(UpdateMeDto body)
    {
        var userId = currentUser.RequireUserId();
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null) return NotFound();

        if (body.DisplayName is not null) profile.DisplayName = body.DisplayName;
        if (body.AvatarUrl is not null) profile.AvatarUrl = body.AvatarUrl;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("api/v1/me/progress")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TopicProgressDto>>> Progress()
    {
        var userId = currentUser.RequireUserId();
        var items = await db.UserTopicProgress
            .Where(p => p.UserId == userId)
            .Select(p => new TopicProgressDto(p.TopicId, p.Status, p.Score, p.Attempts, p.LastAttemptedAt))
            .ToListAsync();
        return items;
    }
}
