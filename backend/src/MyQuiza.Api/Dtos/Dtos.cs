namespace MyQuiza.Api.Dtos;

// ---- Content tree ----
public record SubjectDto(Guid Id, string Name, string Slug, string? Description, string? Icon, string? Category, int? OrderIndex, bool IsDisabled = false, int? CategoryPriority = null);
public record ChapterDto(Guid Id, Guid? SubjectId, string Name, int Form, int OrderIndex, string? Description = null);
public record TopicDto(Guid Id, Guid? ChapterId, string Name, string? Description, int? DifficultyLevel, int? TimeEstimateMinutes, int OrderIndex);
public record TopicBreadcrumbDto(Guid Id, string Name, Guid? ChapterId, string? ChapterName, Guid? SubjectId, string? SubjectName);

// ---- Content tree authoring (moderator-gated, same policy as includeDisabled) ----
// ---- Admin bulk tree read (avoids an O(subjects x chapters) client-side fan-out) ----
public record TopicTreeDto(Guid Id, string Name, int OrderIndex, DateTime CreatedAt, int QuizCount);
public record ChapterTreeDto(Guid Id, string Name, int Form, int OrderIndex, string? Description, int QuizCount, IReadOnlyList<TopicTreeDto> Topics);
public record SubjectTreeDto(Guid Id, string Name, string Slug, string? Description, bool IsDisabled, int QuizCount, IReadOnlyList<ChapterTreeDto> Chapters);

public record CreateSubjectDto(string Name, string Slug, string? Description = null, string? Icon = null, int? OrderIndex = null, string? Category = null, int? CategoryPriority = null);
public record UpdateSubjectDto(string? Name, string? Slug, string? Description, string? Icon, int? OrderIndex, string? Category, int? CategoryPriority, bool? IsDisabled);
public record CreateChapterDto(Guid SubjectId, string Name, int Form, int OrderIndex, string? Description = null);
public record UpdateChapterDto(string? Name, int? Form, int? OrderIndex, string? Description);
public record CreateTopicDto(Guid ChapterId, string Name, string? Description = null, int? DifficultyLevel = null, int? TimeEstimateMinutes = null, int OrderIndex = 0);
public record UpdateTopicDto(string? Name, string? Description, int? DifficultyLevel, int? TimeEstimateMinutes, int? OrderIndex);
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

// ---- Moderation audit trail ----
// commentType: 'suggestion' | 'issue' | 'approved' | 'rejected' (DB-constrained)
public record CreateAuditCommentDto(string CommentText, string CommentType);
public record AuditCommentDto(Guid Id, Guid AdminUserId, string CommentText, string CommentType, bool IsResolved, DateTime CreatedAt, DateTime UpdatedAt);
public record ResolveAuditCommentDto(bool IsResolved);
// action: 'verified' | 'unverified' | 'rejected' — written automatically by the verify endpoint
public record VerificationLogEntryDto(Guid Id, Guid AdminUserId, string Action, string? Reason, DateTime CreatedAt);
// Cross-quiz moderator dashboard aggregate — "today" is UTC calendar day.
public record AuditSummaryDto(int UnresolvedQuizComments, int UnresolvedQuestionComments, int UnresolvedAnswerComments, int VerifiedToday, int UnverifiedToday, int RejectedToday, int UnverifiedQuizCount);
// Review queue: quizzes awaiting moderation, with denormalized ancestry + rolled-up
// unresolved-comment count (quiz + question + answer comments, all attributed to the quiz).
public record UnverifiedQuizDto(Guid Id, string Name, Guid TopicId, string? TopicName, Guid? ChapterId, string? ChapterName, Guid? SubjectId, string? SubjectName, DateTime? CreatedAt, int UnresolvedCommentCount);

// ---- Attempts ----
public record SubmitAnswerDto(Guid QuestionId, IReadOnlyList<Guid> SelectedAnswerIds);
public record SubmitAttemptDto(IReadOnlyList<SubmitAnswerDto> Answers, int? TimeTaken);
// Per-question correctness + the correct answer ids, returned ONLY in the attempt
// response (post-submission) — never on quiz-detail, so the answer key stays hidden
// until the user submits. CorrectAnswerIds lets the client highlight the right options.
public record QuestionResultDto(Guid QuestionId, bool Correct, IReadOnlyList<Guid> CorrectAnswerIds);
public record AttemptResultDto(Guid AttemptId, int Score, int CorrectAnswers, int TotalQuestions, int MaxScore, bool XpAwarded, IReadOnlyList<QuestionResultDto> Questions);
public record AttemptSummaryDto(Guid Id, Guid QuizId, string? QuizTitle, string? Topic, string? Subject, int Score, int CorrectAnswers, int TotalQuestions, DateTime CreatedAt);

// ---- Achievements ----
public record AchievementDto(Guid Id, string AchievementType, string Title, string Description, string Icon, DateTime EarnedAt, int? Progress, int? MaxProgress);

// Catalog: fixed achievement definitions (title/description/icon shown consistently
// across all users). Awarding either references a catalog entry (copies its fields
// as a snapshot) or stays freeform for one-off awards with no catalog definition.
public record AchievementCatalogDto(Guid Id, string AchievementType, string Title, string Description, string Icon, int? MaxProgress);
public record CreateAchievementCatalogDto(string AchievementType, string Title, string Description, string Icon, int? MaxProgress = null);
public record UpdateAchievementCatalogDto(string? AchievementType, string? Title, string? Description, string? Icon, int? MaxProgress);

// Award: pass AchievementId to pull from the catalog (Title/Description/Icon/AchievementType
// ignored if set), or omit it and supply the freeform fields directly for a one-off award.
public record AwardAchievementDto(Guid? AchievementId, string? AchievementType, string? Title, string? Description, string? Icon, int? Progress = null, int? MaxProgress = null);

// ---- Schools ----
public record SchoolStatsDto(decimal AverageScore, decimal ParticipationRate, int TotalQuizzesTaken, int TotalQuestionsAnswered, int CorrectAnswers, int ActiveStudents, DateTime LastCalculatedAt);
// Ranked by average_score desc — flag to EduBridge if a different ranking metric is expected.
public record SchoolLeaderboardEntryDto(Guid Id, string Name, string Type, string District, string State, decimal AverageScore, decimal ParticipationRate, int ActiveStudents);
public record SchoolDetailDto(Guid Id, string Name, string Type, string? Code, string District, string State, string? Address, string? Website, string? Phone, string? PrincipalName, int? TotalStudents, SchoolStatsDto? Stats);
// type is DB-constrained: SMK | SMKA | MRSM | Sekolah Sains | Sekolah Sukan | Sekolah Seni | SBP | SMJK | KV
public record CreateSchoolDto(string Name, string Type, string District, string State, string? Code = null, string? Address = null, string? Website = null, string? Phone = null, string? PrincipalName = null, int? TotalStudents = null);
public record UpdateSchoolDto(string? Name, string? Type, string? Code, string? District, string? State, string? Address, string? Website, string? Phone, string? PrincipalName, int? TotalStudents);

// ---- Dashboard stats (backed by the mv_user_dashboard_stats materialized view) ----
public record DashboardStatsDto(int CompletedQuizzes, decimal AverageScore, int ActiveDays, int WeeklyQuizzes, decimal WeeklyAverageScore, DateTime? LastQuizDate);

// ---- Me / progress / leaderboard ----
public record MeDto(Guid Id, string? DisplayName, string? AvatarUrl, int Xp, int Level, int Streak, string? SchoolRole, string PlatformRole);
// schoolRole is deliberately excluded — it's a privilege lever (teacher/admin grant
// Moderator/Admin via RoleAuthorizationHandler), so it must never be self-service.
public record UpdateMeDto(string? DisplayName, string? AvatarUrl);
public record TopicProgressDto(Guid? TopicId, string? Status, int? Score, int? Attempts, DateTime? LastAttemptedAt);
public record LeaderboardEntryDto(Guid UserId, string? DisplayName, string? AvatarUrl, int Xp, int Level, int WeeklyXp);
