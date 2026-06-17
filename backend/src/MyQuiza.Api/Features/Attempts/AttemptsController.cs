using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Auth;
using MyQuiza.Api.Data;
using MyQuiza.Api.Dtos;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Features.Attempts;

[ApiController]
public class AttemptsController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Submit answers for a quiz. Scoring is done SERVER-SIDE (the client never sees is_correct),
    /// the attempt is recorded, topic progress is upserted, and XP is awarded for verified quizzes.
    /// </summary>
    [HttpPost("api/v1/quizzes/{quizId:guid}/attempts")]
    [Authorize]
    public async Task<ActionResult<AttemptResultDto>> Submit(Guid quizId, SubmitAttemptDto body)
    {
        var userId = currentUser.RequireUserId();

        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Answers)
            .Include(q => q.Topic)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz is null) return NotFound();

        var submitted = body.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedAnswerIds.ToHashSet());

        var total = quiz.Questions.Count;
        var correct = 0;
        var breakdown = new List<QuestionResultDto>(total);
        foreach (var question in quiz.Questions.OrderBy(q => q.OrderIndex))
        {
            var correctSet = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToHashSet();
            var chosen = submitted.TryGetValue(question.Id, out var c) ? c : [];
            // A question with no correct answers defined is malformed and can never be
            // scored correct (keeps `correct` identical to the previous skip behaviour).
            var isCorrect = correctSet.Count > 0 && chosen.SetEquals(correctSet);
            if (isCorrect) correct++;
            breakdown.Add(new QuestionResultDto(question.Id, isCorrect));
        }

        var score = total == 0 ? 0 : (int)Math.Round(correct * 100.0 / total);
        var now = DateTime.UtcNow;
        var isVerified = quiz.Verified ?? false;

        await using var tx = await db.Database.BeginTransactionAsync();

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            UserId = userId,
            Score = score,
            Completed = true,
            QuizTitle = quiz.Name,
            Topic = quiz.Topic?.Name,
            TimeTaken = body.TimeTaken,
            MaxScore = 100,
            CorrectAnswers = correct,
            TotalQuestions = total,
            IsVerifiedQuiz = isVerified,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.QuizAttempts.Add(attempt);

        // Only VERIFIED quizzes contribute to topic progress (business rule). Attempts on
        // unverified quizzes are still recorded above (history) and scored, but don't
        // upsert user_topic_progress, so practice on pending-review quizzes can't mark a
        // topic complete or overwrite its score.
        if (isVerified)
        {
            // Upsert topic progress (unique on user_id + topic_id).
            var status = score >= 70 ? "completed" : "in_progress";
            var progress = await db.UserTopicProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.TopicId == quiz.TopicId);
            if (progress is null)
            {
                db.UserTopicProgress.Add(new UserTopicProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TopicId = quiz.TopicId,
                    Status = status,
                    LastAttemptedAt = now,
                    Score = score,
                    Attempts = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            else
            {
                progress.Status = status;
                progress.LastAttemptedAt = now;
                progress.Score = score;
                progress.Attempts = (progress.Attempts ?? 0) + 1;
                progress.UpdatedAt = now;
            }
        }

        // XP is only awarded for passing a VERIFIED quiz (mirrors EduBridge's rules: +50 XP).
        var xpAwarded = false;
        if (isVerified && score >= 70)
        {
            var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId);
            if (profile is not null)
            {
                const int xp = 50;
                profile.Xp += xp;
                profile.DailyXp += xp;
                profile.WeeklyXp += xp;
                profile.Level = 1 + profile.Xp / 100;
                profile.LastQuizDate = now;
                profile.UpdatedAt = now;
                xpAwarded = true;
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new AttemptResultDto(attempt.Id, score, correct, total, 100, xpAwarded, breakdown);
    }

    [HttpGet("api/v1/me/attempts")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AttemptSummaryDto>>> MyAttempts()
    {
        var userId = currentUser.RequireUserId();
        var items = await db.QuizAttempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttemptSummaryDto(a.Id, a.QuizId, a.QuizTitle, a.Score, a.CorrectAnswers, a.TotalQuestions, a.CreatedAt))
            .ToListAsync();
        return items;
    }
}
