using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;

namespace MyQuiza.Api.Features.Schools;

[ApiController]
[AllowAnonymous]
public class SchoolsController(AppDbContext db) : ControllerBase
{
    /// <summary>Ranked by average_score desc. Schools without a stats row yet sort last.</summary>
    [HttpGet("api/v1/schools")]
    public async Task<ActionResult<IEnumerable<SchoolLeaderboardEntryDto>>> List()
    {
        var items = await db.Schools
            .OrderByDescending(s => s.Stats != null ? s.Stats.AverageScore : -1)
            .Select(s => new SchoolLeaderboardEntryDto(
                s.Id, s.Name, s.Type, s.District, s.State,
                s.Stats != null ? s.Stats.AverageScore : 0,
                s.Stats != null ? s.Stats.ParticipationRate : 0,
                s.Stats != null ? s.Stats.ActiveStudents : 0))
            .ToListAsync();
        return items;
    }

    [HttpGet("api/v1/schools/{id:guid}")]
    public async Task<ActionResult<SchoolDetailDto>> GetById(Guid id)
    {
        var school = await db.Schools.Include(s => s.Stats).AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
        if (school is null) return NotFound();

        var stats = school.Stats is null ? null : new SchoolStatsDto(
            school.Stats.AverageScore, school.Stats.ParticipationRate, school.Stats.TotalQuizzesTaken,
            school.Stats.TotalQuestionsAnswered, school.Stats.CorrectAnswers, school.Stats.ActiveStudents,
            school.Stats.LastCalculatedAt);

        return new SchoolDetailDto(school.Id, school.Name, school.Type, school.Code, school.District,
            school.State, school.Address, school.Website, school.Phone, school.PrincipalName,
            school.TotalStudents, stats);
    }
}
