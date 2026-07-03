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

    /// <summary>
    /// Aggregate quiz stats from mv_user_dashboard_stats. streak/xp/level are already on
    /// GET /me — not duplicated here. Returns zeroed stats (not 404) if the user has no
    /// row yet in the view (e.g. never attempted a quiz).
    /// </summary>
    [HttpGet("api/v1/me/stats")]
    [Authorize]
    public async Task<ActionResult<DashboardStatsDto>> Stats()
    {
        var userId = currentUser.RequireUserId();
        var row = await db.UserDashboardStats.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (row is null) return new DashboardStatsDto(0, 0, 0, 0, 0, null);

        return new DashboardStatsDto((int)row.CompletedQuizzes, row.AverageScore, (int)row.ActiveDays,
            (int)row.WeeklyQuizzes, row.WeeklyAverageScore, row.LastQuizDate);
    }

    [HttpGet("api/v1/me/achievements")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AchievementDto>>> Achievements()
    {
        var userId = currentUser.RequireUserId();
        var items = await db.Achievements
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.EarnedAt)
            .Select(a => new AchievementDto(a.Id, a.AchievementType, a.Title, a.Description, a.Icon, a.EarnedAt, a.Progress, a.MaxProgress))
            .ToListAsync();
        return items;
    }
}
