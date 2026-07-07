using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

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
            .Select(s => new SubjectDto(s.Id, s.Name, s.Slug, s.Description, s.Icon, s.Category, s.OrderIndex, s.IsDisabled, s.CategoryPriority))
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
    public async Task<ActionResult<IEnumerable<QuizSummaryDto>>> Quizzes(
        Guid id, [FromQuery] bool includeUnverified = false)
    {
        var query = db.Quizzes.Where(q => q.TopicId == id);
        if (!includeUnverified) query = query.Where(q => q.Verified == true);

        // Verified quizzes first, then alphabetical — so unverified ones sort last
        // for the client to badge. The `Verified` field reflects the real value now
        // (it was previously hard-coded true since only verified were returned).
        var items = await query
            .OrderByDescending(q => q.Verified == true).ThenBy(q => q.Name)
            .Select(q => new QuizSummaryDto(q.Id, q.TopicId, q.Name, q.Verified == true, q.Questions.Count, q.Difficulty, q.IsPublic))
            .ToListAsync();
        return items;
    }

    /// <summary>
    /// Full nested subjects -> chapters -> topics tree in one call, for admin UIs that
    /// would otherwise fan out into subjects * chapters individual requests. Includes
    /// disabled subjects (moderator-only) since admin tooling needs to see everything.
    /// </summary>
    [HttpGet("api/v1/content-tree")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<IEnumerable<SubjectTreeDto>>> ContentTree()
    {
        var subjects = await db.Subjects
            .Include(s => s.Chapters).ThenInclude(c => c.Topics)
            .AsNoTracking()
            .OrderBy(s => s.CategoryPriority ?? 999).ThenBy(s => s.OrderIndex ?? 0).ThenBy(s => s.Name)
            .ToListAsync();

        var tree = subjects.Select(s => new SubjectTreeDto(
            s.Id, s.Name, s.Slug, s.IsDisabled,
            s.Chapters.OrderBy(c => c.OrderIndex).Select(c => new ChapterTreeDto(
                c.Id, c.Name, c.Form, c.OrderIndex,
                c.Topics.OrderBy(t => t.OrderIndex).Select(t => new TopicTreeDto(t.Id, t.Name, t.OrderIndex)).ToList()
            )).ToList()
        )).ToList();

        return tree;
    }

    // ---- Subjects ----

    [HttpPost("api/v1/subjects")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<SubjectDto>> CreateSubject(CreateSubjectDto body)
    {
        var now = DateTime.UtcNow;
        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = body.Name,
            Slug = body.Slug,
            Description = body.Description,
            Icon = body.Icon,
            OrderIndex = body.OrderIndex,
            Category = body.Category,
            CategoryPriority = body.CategoryPriority,
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        return new SubjectDto(subject.Id, subject.Name, subject.Slug, subject.Description, subject.Icon, subject.Category, subject.OrderIndex, subject.IsDisabled, subject.CategoryPriority);
    }

    [HttpPatch("api/v1/subjects/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> UpdateSubject(Guid id, UpdateSubjectDto body)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == id);
        if (subject is null) return NotFound();

        if (body.Name is not null) subject.Name = body.Name;
        if (body.Slug is not null) subject.Slug = body.Slug;
        if (body.Description is not null) subject.Description = body.Description;
        if (body.Icon is not null) subject.Icon = body.Icon;
        if (body.OrderIndex is not null) subject.OrderIndex = body.OrderIndex;
        if (body.Category is not null) subject.Category = body.Category;
        if (body.CategoryPriority is not null) subject.CategoryPriority = body.CategoryPriority;
        if (body.IsDisabled is not null) subject.IsDisabled = body.IsDisabled.Value;
        subject.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Refuses to delete while chapters exist rather than relying on the DB's FK behavior —
    /// subjects.chapters has no ON DELETE action, so an unguarded delete would 500 with a
    /// raw constraint violation instead of a clean, actionable response.
    /// </summary>
    [HttpDelete("api/v1/subjects/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> DeleteSubject(Guid id)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == id);
        if (subject is null) return NotFound();

        var chapterCount = await db.Chapters.CountAsync(c => c.SubjectId == id);
        if (chapterCount > 0)
            return Conflict($"Subject has {chapterCount} chapter(s). Delete or move them first.");

        db.Subjects.Remove(subject);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ---- Chapters ----

    [HttpPost("api/v1/chapters")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<ChapterDto>> CreateChapter(CreateChapterDto body)
    {
        var now = DateTime.UtcNow;
        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            SubjectId = body.SubjectId,
            Name = body.Name,
            Form = body.Form,
            OrderIndex = body.OrderIndex,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Chapters.Add(chapter);
        await db.SaveChangesAsync();

        return new ChapterDto(chapter.Id, chapter.SubjectId, chapter.Name, chapter.Form, chapter.OrderIndex);
    }

    [HttpPatch("api/v1/chapters/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> UpdateChapter(Guid id, UpdateChapterDto body)
    {
        var chapter = await db.Chapters.FirstOrDefaultAsync(c => c.Id == id);
        if (chapter is null) return NotFound();

        if (body.Name is not null) chapter.Name = body.Name;
        if (body.Form is not null) chapter.Form = body.Form.Value;
        if (body.OrderIndex is not null) chapter.OrderIndex = body.OrderIndex.Value;
        chapter.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Refuses to delete while topics exist. Unlike subjects, chapters.topics DOES cascade
    /// in the DB (topics -> quizzes -> questions/answers, plus quiz_attempts and
    /// user_topic_progress) — an unguarded delete here would silently destroy every quiz,
    /// question, and student's attempt history under this chapter with a single call.
    /// </summary>
    [HttpDelete("api/v1/chapters/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> DeleteChapter(Guid id)
    {
        var chapter = await db.Chapters.FirstOrDefaultAsync(c => c.Id == id);
        if (chapter is null) return NotFound();

        var topicCount = await db.Topics.CountAsync(t => t.ChapterId == id);
        if (topicCount > 0)
            return Conflict($"Chapter has {topicCount} topic(s). Deleting it would cascade-delete their quizzes, questions, and student attempt history — delete or move the topics first.");

        db.Chapters.Remove(chapter);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ---- Topics ----

    [HttpPost("api/v1/topics")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<TopicDto>> CreateTopic(CreateTopicDto body)
    {
        var now = DateTime.UtcNow;
        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            ChapterId = body.ChapterId,
            Name = body.Name,
            Description = body.Description,
            DifficultyLevel = body.DifficultyLevel,
            TimeEstimateMinutes = body.TimeEstimateMinutes,
            OrderIndex = body.OrderIndex,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        return new TopicDto(topic.Id, topic.ChapterId, topic.Name, topic.Description, topic.DifficultyLevel, topic.TimeEstimateMinutes, topic.OrderIndex);
    }

    [HttpPatch("api/v1/topics/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> UpdateTopic(Guid id, UpdateTopicDto body)
    {
        var topic = await db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic is null) return NotFound();

        if (body.Name is not null) topic.Name = body.Name;
        if (body.Description is not null) topic.Description = body.Description;
        if (body.DifficultyLevel is not null) topic.DifficultyLevel = body.DifficultyLevel;
        if (body.TimeEstimateMinutes is not null) topic.TimeEstimateMinutes = body.TimeEstimateMinutes;
        if (body.OrderIndex is not null) topic.OrderIndex = body.OrderIndex.Value;
        topic.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Refuses to delete while quizzes exist under this topic. quizzes.topic_id cascades
    /// in the DB to questions/answers plus quiz_attempts and user_topic_progress — an
    /// unguarded delete would silently wipe every quiz and student's history for this topic.
    /// </summary>
    [HttpDelete("api/v1/topics/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> DeleteTopic(Guid id)
    {
        var topic = await db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic is null) return NotFound();

        var quizCount = await db.Quizzes.CountAsync(q => q.TopicId == id);
        if (quizCount > 0)
            return Conflict($"Topic has {quizCount} quiz(zes). Deleting it would cascade-delete them and student attempt history — delete or move the quizzes first.");

        db.Topics.Remove(topic);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
