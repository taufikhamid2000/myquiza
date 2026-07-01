namespace MyQuiza.Api.Dtos;

// ---- Content tree ----
public record SubjectDto(Guid Id, string Name, string Slug, string? Description, string? Icon, string? Category, int? OrderIndex, bool IsDisabled = false);
public record ChapterDto(Guid Id, Guid? SubjectId, string Name, int Form, int OrderIndex);
public record TopicDto(Guid Id, Guid? ChapterId, string Name, string? Description, int? DifficultyLevel, int? TimeEstimateMinutes, int OrderIndex);
public record QuizSummaryDto(Guid Id, Guid TopicId, string Name, bool Verified, int QuestionCount, string? Difficulty, bool IsPublic);

// ---- Quiz detail (TAKER-facing: is_correct is intentionally NOT exposed) ----
public record AnswerOptionDto(Guid Id, string Text, int OrderIndex);
public record QuestionDto(Guid Id, string Text, string Type, int OrderIndex, IReadOnlyList<AnswerOptionDto> Options);
public record QuizDetailDto(Guid Id, Guid TopicId, string Name, bool Verified, int? TimeLimit, string? Difficulty, bool IsPublic, IReadOnlyList<QuestionDto> Questions);

// ---- Quiz authoring ----
public record CreateAnswerDto(string Text, bool IsCorrect, int OrderIndex);
public record CreateQuestionDto(string Text, string Type, int OrderIndex, IReadOnlyList<CreateAnswerDto> Answers);
public record CreateQuizDto(Guid TopicId, string Name, IReadOnlyList<CreateQuestionDto> Questions, int? TimeLimit = null, string? Difficulty = null, bool IsPublic = true);
public record VerifyQuizDto(bool Verified, string? Feedback);

// ---- Quiz edit (author/moderator-facing: is_correct IS exposed) ----
public record UpdateQuizDto(string? Name, string? Difficulty, int? TimeLimit, bool? IsPublic);
public record AddQuestionDto(string Text, string Type, int OrderIndex, IReadOnlyList<CreateAnswerDto> Answers);
public record UpdateQuestionDto(string? Text, string? Type, int? OrderIndex);
public record AddAnswerDto(string Text, bool IsCorrect, int OrderIndex);
public record UpdateAnswerDto(string? Text, bool? IsCorrect, int? OrderIndex);
public record AnswerAuthorDto(Guid Id, string Text, bool IsCorrect, int OrderIndex);
public record QuestionAuthorDto(Guid Id, string Text, string Type, int OrderIndex, IReadOnlyList<AnswerAuthorDto> Answers);
public record QuizAuthorDetailDto(Guid Id, Guid TopicId, string Name, bool Verified, int? TimeLimit, string? Difficulty, bool IsPublic, IReadOnlyList<QuestionAuthorDto> Questions);

// ---- Attempts ----
public record SubmitAnswerDto(Guid QuestionId, IReadOnlyList<Guid> SelectedAnswerIds);
public record SubmitAttemptDto(IReadOnlyList<SubmitAnswerDto> Answers, int? TimeTaken);
// Per-question correctness + the correct answer ids, returned ONLY in the attempt
// response (post-submission) — never on quiz-detail, so the answer key stays hidden
// until the user submits. CorrectAnswerIds lets the client highlight the right options.
public record QuestionResultDto(Guid QuestionId, bool Correct, IReadOnlyList<Guid> CorrectAnswerIds);
public record AttemptResultDto(Guid AttemptId, int Score, int CorrectAnswers, int TotalQuestions, int MaxScore, bool XpAwarded, IReadOnlyList<QuestionResultDto> Questions);
public record AttemptSummaryDto(Guid Id, Guid QuizId, string? QuizTitle, int Score, int CorrectAnswers, int TotalQuestions, DateTime CreatedAt);

// ---- Me / progress / leaderboard ----
public record MeDto(Guid Id, string? DisplayName, string? AvatarUrl, int Xp, int Level, int Streak, string? SchoolRole, string PlatformRole);
public record TopicProgressDto(Guid? TopicId, string? Status, int? Score, int? Attempts, DateTime? LastAttemptedAt);
public record LeaderboardEntryDto(Guid UserId, string? DisplayName, string? AvatarUrl, int Xp, int Level, int WeeklyXp);
