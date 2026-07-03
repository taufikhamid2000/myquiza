using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Features.Schools;

[ApiController]
public class SchoolsController(AppDbContext db) : ControllerBase
{
    /// <summary>Ranked by average_score desc. Schools without a stats row yet sort last.</summary>
    [HttpGet("api/v1/schools")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    [HttpPost("api/v1/schools")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<SchoolDetailDto>> Create(CreateSchoolDto body)
    {
        var now = DateTime.UtcNow;
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = body.Name,
            Type = body.Type,
            Code = body.Code,
            District = body.District,
            State = body.State,
            Address = body.Address,
            Website = body.Website,
            Phone = body.Phone,
            PrincipalName = body.PrincipalName,
            TotalStudents = body.TotalStudents,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Schools.Add(school);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = school.Id },
            new SchoolDetailDto(school.Id, school.Name, school.Type, school.Code, school.District,
                school.State, school.Address, school.Website, school.Phone, school.PrincipalName,
                school.TotalStudents, null));
    }

    [HttpPatch("api/v1/schools/{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateSchoolDto body)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.Id == id);
        if (school is null) return NotFound();

        if (body.Name is not null) school.Name = body.Name;
        if (body.Type is not null) school.Type = body.Type;
        if (body.Code is not null) school.Code = body.Code;
        if (body.District is not null) school.District = body.District;
        if (body.State is not null) school.State = body.State;
        if (body.Address is not null) school.Address = body.Address;
        if (body.Website is not null) school.Website = body.Website;
        if (body.Phone is not null) school.Phone = body.Phone;
        if (body.PrincipalName is not null) school.PrincipalName = body.PrincipalName;
        if (body.TotalStudents is not null) school.TotalStudents = body.TotalStudents;
        school.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("api/v1/schools/{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.Id == id);
        if (school is null) return NotFound();

        db.Schools.Remove(school);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
