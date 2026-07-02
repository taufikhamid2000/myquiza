using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Auth;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Features.Quizzes;

/// <summary>
/// Moderation audit trail: reviewer comments on quizzes/questions/answers, plus the
/// verification action log. Adding/resolving comments is Moderator-only; viewing is
/// open to the quiz creator (so they can see reviewer feedback) or any moderator.
/// </summary>
[ApiController]
[Authorize]
public class AuditController(AppDbContext db, CurrentUser currentUser, IAuthorizationService authz) : ControllerBase
{
    // ---- Quiz comments ----

    [HttpGet("api/v1/quizzes/{quizId:guid}/comments")]
    public async Task<ActionResult<IEnumerable<AuditCommentDto>>> GetQuizComments(Guid quizId)
    {
        var userId = currentUser.RequireUserId();
        var quiz = await db.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz is null) return NotFound();
        if (!await IsOwnerOrModerator(quiz, userId)) return Forbid();

        var items = await db.QuizAuditComments.AsNoTracking()
            .Where(c => c.QuizId == quizId).OrderBy(c => c.CreatedAt)
            .Select(c => new AuditCommentDto(c.Id, c.AdminUserId, c.CommentText, c.CommentType, c.IsResolved, c.CreatedAt, c.UpdatedAt))
            .ToListAsync();
        return items;
    }

    [HttpPost("api/v1/quizzes/{quizId:guid}/comments")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<AuditCommentDto>> AddQuizComment(Guid quizId, CreateAuditCommentDto body)
    {
        var userId = currentUser.RequireUserId();
        var quiz = await db.Quizzes.AnyAsync(q => q.Id == quizId);
        if (!quiz) return NotFound();

        var now = DateTime.UtcNow;
        var comment = new QuizAuditComment
        {
            Id = Guid.NewGuid(), QuizId = quizId, AdminUserId = userId,
            CommentText = body.CommentText, CommentType = body.CommentType,
            IsResolved = false, CreatedAt = now, UpdatedAt = now,
        };
        db.QuizAuditComments.Add(comment);
        await db.SaveChangesAsync();

        return new AuditCommentDto(comment.Id, comment.AdminUserId, comment.CommentText, comment.CommentType, comment.IsResolved, comment.CreatedAt, comment.UpdatedAt);
    }

    [HttpPatch("api/v1/quizzes/{quizId:guid}/comments/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> ResolveQuizComment(Guid quizId, Guid id, ResolveAuditCommentDto body)
    {
        var comment = await db.QuizAuditComments.FirstOrDefaultAsync(c => c.Id == id && c.QuizId == quizId);
        if (comment is null) return NotFound();

        comment.IsResolved = body.IsResolved;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Question comments ----

    [HttpGet("api/v1/questions/{questionId:guid}/comments")]
    public async Task<ActionResult<IEnumerable<AuditCommentDto>>> GetQuestionComments(Guid questionId)
    {
        var userId = currentUser.RequireUserId();
        var question = await db.Questions.Include(q => q.Quiz).AsNoTracking().FirstOrDefaultAsync(q => q.Id == questionId);
        if (question is null) return NotFound();
        if (!await IsOwnerOrModerator(question.Quiz!, userId)) return Forbid();

        var items = await db.QuestionAuditComments.AsNoTracking()
            .Where(c => c.QuestionId == questionId).OrderBy(c => c.CreatedAt)
            .Select(c => new AuditCommentDto(c.Id, c.AdminUserId, c.CommentText, c.CommentType, c.IsResolved, c.CreatedAt, c.UpdatedAt))
            .ToListAsync();
        return items;
    }

    [HttpPost("api/v1/questions/{questionId:guid}/comments")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<AuditCommentDto>> AddQuestionComment(Guid questionId, CreateAuditCommentDto body)
    {
        var userId = currentUser.RequireUserId();
        var exists = await db.Questions.AnyAsync(q => q.Id == questionId);
        if (!exists) return NotFound();

        var now = DateTime.UtcNow;
        var comment = new QuestionAuditComment
        {
            Id = Guid.NewGuid(), QuestionId = questionId, AdminUserId = userId,
            CommentText = body.CommentText, CommentType = body.CommentType,
            IsResolved = false, CreatedAt = now, UpdatedAt = now,
        };
        db.QuestionAuditComments.Add(comment);
        await db.SaveChangesAsync();

        return new AuditCommentDto(comment.Id, comment.AdminUserId, comment.CommentText, comment.CommentType, comment.IsResolved, comment.CreatedAt, comment.UpdatedAt);
    }

    [HttpPatch("api/v1/questions/{questionId:guid}/comments/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> ResolveQuestionComment(Guid questionId, Guid id, ResolveAuditCommentDto body)
    {
        var comment = await db.QuestionAuditComments.FirstOrDefaultAsync(c => c.Id == id && c.QuestionId == questionId);
        if (comment is null) return NotFound();

        comment.IsResolved = body.IsResolved;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Answer comments ----

    [HttpGet("api/v1/answers/{answerId:guid}/comments")]
    public async Task<ActionResult<IEnumerable<AuditCommentDto>>> GetAnswerComments(Guid answerId)
    {
        var userId = currentUser.RequireUserId();
        var answer = await db.Answers.Include(a => a.Question).ThenInclude(q => q!.Quiz)
            .AsNoTracking().FirstOrDefaultAsync(a => a.Id == answerId);
        if (answer is null) return NotFound();
        if (!await IsOwnerOrModerator(answer.Question!.Quiz!, userId)) return Forbid();

        var items = await db.AnswerAuditComments.AsNoTracking()
            .Where(c => c.AnswerId == answerId).OrderBy(c => c.CreatedAt)
            .Select(c => new AuditCommentDto(c.Id, c.AdminUserId, c.CommentText, c.CommentType, c.IsResolved, c.CreatedAt, c.UpdatedAt))
            .ToListAsync();
        return items;
    }

    [HttpPost("api/v1/answers/{answerId:guid}/comments")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<AuditCommentDto>> AddAnswerComment(Guid answerId, CreateAuditCommentDto body)
    {
        var userId = currentUser.RequireUserId();
        var exists = await db.Answers.AnyAsync(a => a.Id == answerId);
        if (!exists) return NotFound();

        var now = DateTime.UtcNow;
        var comment = new AnswerAuditComment
        {
            Id = Guid.NewGuid(), AnswerId = answerId, AdminUserId = userId,
            CommentText = body.CommentText, CommentType = body.CommentType,
            IsResolved = false, CreatedAt = now, UpdatedAt = now,
        };
        db.AnswerAuditComments.Add(comment);
        await db.SaveChangesAsync();

        return new AuditCommentDto(comment.Id, comment.AdminUserId, comment.CommentText, comment.CommentType, comment.IsResolved, comment.CreatedAt, comment.UpdatedAt);
    }

    [HttpPatch("api/v1/answers/{answerId:guid}/comments/{id:guid}")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> ResolveAnswerComment(Guid answerId, Guid id, ResolveAuditCommentDto body)
    {
        var comment = await db.AnswerAuditComments.FirstOrDefaultAsync(c => c.Id == id && c.AnswerId == answerId);
        if (comment is null) return NotFound();

        comment.IsResolved = body.IsResolved;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Verification log (read-only; written by POST /quizzes/{id}/verify) ----

    [HttpGet("api/v1/quizzes/{quizId:guid}/verification-log")]
    public async Task<ActionResult<IEnumerable<VerificationLogEntryDto>>> GetVerificationLog(Guid quizId)
    {
        var userId = currentUser.RequireUserId();
        var quiz = await db.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz is null) return NotFound();
        if (!await IsOwnerOrModerator(quiz, userId)) return Forbid();

        var items = await db.QuizVerificationLogs.AsNoTracking()
            .Where(l => l.QuizId == quizId).OrderByDescending(l => l.CreatedAt)
            .Select(l => new VerificationLogEntryDto(l.Id, l.AdminUserId, l.Action, l.Reason, l.CreatedAt))
            .ToListAsync();
        return items;
    }

    private async Task<bool> IsOwnerOrModerator(Quiz quiz, Guid userId)
    {
        if (quiz.CreatedBy == userId.ToString()) return true;
        var result = await authz.AuthorizeAsync(User, "Moderator");
        return result.Succeeded;
    }
}
