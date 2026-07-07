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

    /// <summary>
    /// Cross-quiz moderator dashboard aggregate: unresolved comment counts plus today's
    /// verify/unverify/reject action counts. Every other audit endpoint is scoped to a
    /// single quiz/question/answer, so this exists to avoid an admin UI fan-out.
    /// </summary>
    [HttpGet("api/v1/admin/audit-summary")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<AuditSummaryDto>> AuditSummary()
    {
        var todayUtc = DateTime.UtcNow.Date;

        var unresolvedQuiz = await db.QuizAuditComments.CountAsync(c => !c.IsResolved);
        var unresolvedQuestion = await db.QuestionAuditComments.CountAsync(c => !c.IsResolved);
        var unresolvedAnswer = await db.AnswerAuditComments.CountAsync(c => !c.IsResolved);

        var verifiedToday = await db.QuizVerificationLogs.CountAsync(l => l.Action == "verified" && l.CreatedAt >= todayUtc);
        var unverifiedToday = await db.QuizVerificationLogs.CountAsync(l => l.Action == "unverified" && l.CreatedAt >= todayUtc);
        var rejectedToday = await db.QuizVerificationLogs.CountAsync(l => l.Action == "rejected" && l.CreatedAt >= todayUtc);
        var unverifiedQuizCount = await db.Quizzes.CountAsync(q => q.Verified != true);

        return new AuditSummaryDto(unresolvedQuiz, unresolvedQuestion, unresolvedAnswer, verifiedToday, unverifiedToday, rejectedToday, unverifiedQuizCount);
    }

    /// <summary>
    /// Review queue: all unverified quizzes with ancestry (subject/chapter/topic names)
    /// and a rolled-up unresolved-comment count (quiz + question + answer comments, all
    /// attributed back to the owning quiz). Built to replace an admin-side N+1 that ran
    /// one comment-count query per quiz.
    /// </summary>
    [HttpGet("api/v1/admin/quizzes/unverified")]
    [Authorize(Policy = "Moderator")]
    public async Task<ActionResult<IEnumerable<UnverifiedQuizDto>>> UnverifiedQuizzes()
    {
        var quizCommentCounts = await db.QuizAuditComments
            .Where(c => !c.IsResolved)
            .GroupBy(c => c.QuizId)
            .Select(g => new { QuizId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.QuizId, x => x.Count);

        var questionCommentCounts = await db.QuestionAuditComments
            .Where(c => !c.IsResolved)
            .Join(db.Questions, c => c.QuestionId, q => q.Id, (c, q) => q.QuizId)
            .GroupBy(quizId => quizId)
            .Select(g => new { QuizId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.QuizId, x => x.Count);

        var answerCommentCounts = await db.AnswerAuditComments
            .Where(c => !c.IsResolved)
            .Join(db.Answers, c => c.AnswerId, a => a.Id, (c, a) => a.QuestionId)
            .Join(db.Questions, questionId => questionId, q => q.Id, (questionId, q) => q.QuizId)
            .GroupBy(quizId => quizId)
            .Select(g => new { QuizId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.QuizId, x => x.Count);

        var quizzes = await db.Quizzes
            .Where(q => q.Verified != true)
            .Include(q => q.Topic).ThenInclude(t => t!.Chapter).ThenInclude(c => c!.Subject)
            .AsNoTracking()
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();

        var items = quizzes.Select(q =>
        {
            var unresolvedCount = quizCommentCounts.GetValueOrDefault(q.Id)
                + questionCommentCounts.GetValueOrDefault(q.Id)
                + answerCommentCounts.GetValueOrDefault(q.Id);

            return new UnverifiedQuizDto(q.Id, q.Name, q.TopicId, q.Topic?.Name,
                q.Topic?.ChapterId, q.Topic?.Chapter?.Name,
                q.Topic?.Chapter?.SubjectId, q.Topic?.Chapter?.Subject?.Name,
                q.CreatedAt, unresolvedCount);
        }).ToList();

        return items;
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
