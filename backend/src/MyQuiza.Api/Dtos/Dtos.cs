namespace MyQuiza.Api.Dtos;

// ---- Content tree ----
public record SubjectDto(Guid Id, string Name, string Slug, string? Description, string? Icon, string? Category, int? OrderIndex, bool IsDisabled = false);
public record ChapterDto(Guid Id, Guid? SubjectId, string Name, int Form, int OrderIndex);
public record TopicDto(Guid Id, Guid? ChapterId, string Name, string? Description, int? DifficultyLevel, int? TimeEstimateMinutes, int OrderIndex);
public record QuizSummaryDto(Guid Id, Guid TopicId, string Name, bool Verified, int QuestionCount);

// ---- Quiz detail (TAKER-facing: is_correct is intentionally NOT exposed) ----
public record AnswerOptionDto(Guid Id, string Text, int OrderIndex);
public record QuestionDto(Guid Id, string Text, string Type, int OrderIndex, IReadOnlyList<AnswerOptionDto> Options);
public record QuizDetailDto(Guid Id, Guid TopicId, string Name, bool Verified, IReadOnlyList<QuestionDto> Questions);

// ---- Quiz authoring ----
public record CreateAnswerDto(string Text, bool IsCorrect, int OrderIndex);
public record CreateQuestionDto(string Text, string Type, int OrderIndex, IReadOnlyList<CreateAnswerDto> Answers);
public record CreateQuizDto(Guid TopicId, string Name, IReadOnlyList<CreateQuestionDto> Questions);
public record VerifyQuizDto(bool Verified, string? Feedback);

// ---- Attempts ----
public record SubmitAnswerDto(Guid QuestionId, IReadOnlyList<Guid> SelectedAnswerIds);
public record SubmitAttemptDto(IReadOnlyList<SubmitAnswerDto> Answers, int? TimeTaken);
public record AttemptResultDto(Guid AttemptId, int Score, int CorrectAnswers, int TotalQuestions, int MaxScore, bool XpAwarded);
public record AttemptSummaryDto(Guid Id, Guid QuizId, string? QuizTitle, int Score, int CorrectAnswers, int TotalQuestions, DateTime CreatedAt);

// ---- Me / progress / leaderboard ----
public record MeDto(Guid Id, string? DisplayName, string? AvatarUrl, int Xp, int Level, int Streak, string? SchoolRole, string PlatformRole);
public record TopicProgressDto(Guid? TopicId, string? Status, int? Score, int? Attempts, DateTime? LastAttemptedAt);
public record LeaderboardEntryDto(Guid UserId, string? DisplayName, string? AvatarUrl, int Xp, int Level, int WeeklyXp);
