using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;

namespace MyQuiza.Api.Features.Leaderboard;

[ApiController]
public class LeaderboardController(AppDbContext db) : ControllerBase
{
    /// <summary>Top users by XP (or weekly XP). Excludes disabled profiles.</summary>
    [HttpGet("api/v1/leaderboard")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> Get(
        [FromQuery] int limit = 20, [FromQuery] string period = "all")
    {
        limit = Math.Clamp(limit, 1, 100);

        var query = db.UserProfiles.Where(p => !p.IsDisabled);
        query = period == "weekly"
            ? query.OrderByDescending(p => p.WeeklyXp)
            : query.OrderByDescending(p => p.Xp);

        var items = await query
            .Take(limit)
            .Select(p => new LeaderboardEntryDto(p.Id, p.DisplayName, p.AvatarUrl, p.Xp, p.Level, p.WeeklyXp))
            .ToListAsync();
        return items;
    }
}
