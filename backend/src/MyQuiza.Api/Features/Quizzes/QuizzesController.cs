using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Auth;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Features.Quizzes;

[ApiController]
public class QuizzesController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    /// <summary>Quiz for taking — answer options are returned WITHOUT is_correct.</summary>
    [HttpGet("api/v1/quizzes/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<QuizDetailDto>> GetById(Guid id)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Answers)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();

        var questions = quiz.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q => new QuestionDto(
                q.Id, q.Text, q.Type, q.OrderIndex,
                q.Answers.OrderBy(a => a.OrderIndex)
                    .Select(a => new AnswerOptionDto(a.Id, a.Text, a.OrderIndex))
                    .ToList()))
            .ToList();

        return new QuizDetailDto(quiz.Id, quiz.TopicId, quiz.Name, quiz.Verified ?? false, quiz.TimeLimit, questions);
    }

    [HttpPost("api/v1/quizzes")]
    [Authorize]
    public async Task<ActionResult<QuizSummaryDto>> Create(CreateQuizDto body)
    {
        var userId = currentUser.RequireUserId();
        var now = DateTime.UtcNow;

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            TopicId = body.TopicId,
            Name = body.Name,
            CreatedBy = userId.ToString(),
            Verified = false,
            CreatedAt = now,
            Questions = body.Questions.Select(q => new Question
            {
                Id = Guid.NewGuid(),
                Text = q.Text,
                Type = q.Type,
                OrderIndex = q.OrderIndex,
                CreatedAt = now,
                UpdatedAt = now,
                Answers = q.Answers.Select(a => new Answer
                {
                    Id = Guid.NewGuid(),
                    Text = a.Text,
                    IsCorrect = a.IsCorrect,
                    OrderIndex = a.OrderIndex,
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToList(),
            }).ToList(),
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = quiz.Id },
            new QuizSummaryDto(quiz.Id, quiz.TopicId, quiz.Name, false, quiz.Questions.Count));
    }

    /// <summary>Verify/unverify a quiz — moderators &amp; admins only.</summary>
    [HttpPost("api/v1/quizzes/{id:guid}/verify")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> Verify(Guid id, VerifyQuizDto body)
    {
        var userId = currentUser.RequireUserId();
        var quiz = await db.Quizzes.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return NotFound();

        quiz.Verified = body.Verified;
        quiz.VerifiedBy = userId;
        quiz.VerifiedAt = DateTime.UtcNow;
        quiz.VerificationFeedback = body.Feedback;
        await db.SaveChangesAsync();

        return NoContent();
    }
}
