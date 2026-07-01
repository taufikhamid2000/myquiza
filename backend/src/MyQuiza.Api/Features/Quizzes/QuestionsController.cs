using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Auth;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Features.Quizzes;

[ApiController]
[Authorize]
public class QuestionsController(AppDbContext db, CurrentUser currentUser, IAuthorizationService authz) : ControllerBase
{
    // ---- Questions ----

    [HttpPost("api/v1/quizzes/{quizId:guid}/questions")]
    public async Task<ActionResult<QuestionAuthorDto>> AddQuestion(Guid quizId, AddQuestionDto body)
    {
        var userId = currentUser.RequireUserId();
        var quiz = await db.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz is null) return NotFound();
        if (!await IsOwnerOrModerator(quiz, userId)) return Forbid();

        var now = DateTime.UtcNow;
        var question = new Question
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            Text = body.Text,
            Type = body.Type,
            OrderIndex = body.OrderIndex,
            CreatedAt = now,
            UpdatedAt = now,
            Answers = body.Answers.Select(a => new Answer
            {
                Id = Guid.NewGuid(),
                Text = a.Text,
                IsCorrect = a.IsCorrect,
                OrderIndex = a.OrderIndex,
                CreatedAt = now,
                UpdatedAt = now,
            }).ToList(),
        };

        db.Questions.Add(question);
        quiz.Verified = false;
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestion), new { id = question.Id },
            ToAuthorDto(question));
    }

    [HttpGet("api/v1/questions/{id:guid}")]
    public async Task<ActionResult<QuestionAuthorDto>> GetQuestion(Guid id)
    {
        var userId = currentUser.RequireUserId();
        var question = await db.Questions
            .Include(q => q.Answers)
            .Include(q => q.Quiz)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound();
        if (!await IsOwnerOrModerator(question.Quiz!, userId)) return Forbid();

        return ToAuthorDto(question);
    }

    [HttpPatch("api/v1/questions/{id:guid}")]
    public async Task<IActionResult> UpdateQuestion(Guid id, UpdateQuestionDto body)
    {
        var userId = currentUser.RequireUserId();
        var question = await db.Questions
            .Include(q => q.Quiz)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound();
        if (!await IsOwnerOrModerator(question.Quiz!, userId)) return Forbid();

        if (body.Text is not null) question.Text = body.Text;
        if (body.Type is not null) question.Type = body.Type;
        if (body.OrderIndex is not null) question.OrderIndex = body.OrderIndex.Value;
        question.UpdatedAt = DateTime.UtcNow;
        question.Quiz!.Verified = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("api/v1/questions/{id:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        var userId = currentUser.RequireUserId();
        var question = await db.Questions
            .Include(q => q.Answers)
            .Include(q => q.Quiz)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound();
        if (!await IsOwnerOrModerator(question.Quiz!, userId)) return Forbid();

        db.Answers.RemoveRange(question.Answers);
        db.Questions.Remove(question);
        question.Quiz!.Verified = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ---- Answers ----

    [HttpPost("api/v1/questions/{questionId:guid}/answers")]
    public async Task<ActionResult<AnswerAuthorDto>> AddAnswer(Guid questionId, AddAnswerDto body)
    {
        var userId = currentUser.RequireUserId();
        var question = await db.Questions
            .Include(q => q.Quiz)
            .FirstOrDefaultAsync(q => q.Id == questionId);
        if (question is null) return NotFound();
        if (!await IsOwnerOrModerator(question.Quiz!, userId)) return Forbid();

        var now = DateTime.UtcNow;
        var answer = new Answer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Text = body.Text,
            IsCorrect = body.IsCorrect,
            OrderIndex = body.OrderIndex,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Answers.Add(answer);
        question.Quiz!.Verified = false;
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestion), new { id = questionId },
            new AnswerAuthorDto(answer.Id, answer.Text, answer.IsCorrect, answer.OrderIndex));
    }

    [HttpPatch("api/v1/answers/{id:guid}")]
    public async Task<IActionResult> UpdateAnswer(Guid id, UpdateAnswerDto body)
    {
        var userId = currentUser.RequireUserId();
        var answer = await db.Answers
            .Include(a => a.Question).ThenInclude(q => q!.Quiz)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (answer is null) return NotFound();
        if (!await IsOwnerOrModerator(answer.Question!.Quiz!, userId)) return Forbid();

        if (body.Text is not null) answer.Text = body.Text;
        if (body.IsCorrect is not null) answer.IsCorrect = body.IsCorrect.Value;
        if (body.OrderIndex is not null) answer.OrderIndex = body.OrderIndex.Value;
        answer.UpdatedAt = DateTime.UtcNow;
        answer.Question!.Quiz!.Verified = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("api/v1/answers/{id:guid}")]
    public async Task<IActionResult> DeleteAnswer(Guid id)
    {
        var userId = currentUser.RequireUserId();
        var answer = await db.Answers
            .Include(a => a.Question).ThenInclude(q => q!.Quiz)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (answer is null) return NotFound();
        if (!await IsOwnerOrModerator(answer.Question!.Quiz!, userId)) return Forbid();

        db.Answers.Remove(answer);
        answer.Question!.Quiz!.Verified = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> IsOwnerOrModerator(Quiz quiz, Guid userId)
    {
        if (quiz.CreatedBy == userId.ToString()) return true;
        var result = await authz.AuthorizeAsync(User, "Moderator");
        return result.Succeeded;
    }

    private static QuestionAuthorDto ToAuthorDto(Question q) =>
        new(q.Id, q.Text, q.Type, q.OrderIndex,
            q.Answers.OrderBy(a => a.OrderIndex)
                .Select(a => new AnswerAuthorDto(a.Id, a.Text, a.IsCorrect, a.OrderIndex))
                .ToList());
}
