using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;

namespace MyQuiza.Api.Features.Content;

[ApiController]
public class ContentController(AppDbContext db, IAuthorizationService authz) : ControllerBase
{
    [HttpGet("api/v1/subjects")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> Subjects([FromQuery] bool includeDisabled = false)
    {
        if (includeDisabled)
        {
            var result = await authz.AuthorizeAsync(User, "Moderator");
            if (!result.Succeeded) return Forbid();
        }

        var query = db.Subjects.AsQueryable();
        if (!includeDisabled) query = query.Where(s => !s.IsDisabled);

        var items = await query
            .OrderBy(s => s.CategoryPriority ?? 999).ThenBy(s => s.OrderIndex ?? 0).ThenBy(s => s.Name)
            .Select(s => new SubjectDto(s.Id, s.Name, s.Slug, s.Description, s.Icon, s.Category, s.OrderIndex, s.IsDisabled))
            .ToListAsync();
        return items;
    }

    [HttpGet("api/v1/subjects/{id:guid}/chapters")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ChapterDto>>> Chapters(Guid id)
    {
        var items = await db.Chapters
            .Where(c => c.SubjectId == id)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new ChapterDto(c.Id, c.SubjectId, c.Name, c.Form, c.OrderIndex))
            .ToListAsync();
        return items;
    }

    [HttpGet("api/v1/chapters/{id:guid}/topics")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TopicDto>>> Topics(Guid id)
    {
        var items = await db.Topics
            .Where(t => t.ChapterId == id)
            .OrderBy(t => t.OrderIndex)
            .Select(t => new TopicDto(t.Id, t.ChapterId, t.Name, t.Description, t.DifficultyLevel, t.TimeEstimateMinutes, t.OrderIndex))
            .ToListAsync();
        return items;
    }

    [HttpGet("api/v1/topics/{id:guid}/quizzes")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<QuizSummaryDto>>> Quizzes(Guid id)
    {
        var items = await db.Quizzes
            .Where(q => q.TopicId == id && q.Verified == true)
            .Select(q => new QuizSummaryDto(q.Id, q.TopicId, q.Name, true, q.Questions.Count))
            .ToListAsync();
        return items;
    }
}
