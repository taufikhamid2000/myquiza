namespace MyQuiza.Api.Models;

// User / attempt / progress entities — mapped to EduBridge's existing Supabase tables.
// user ids are Supabase auth.users ids (uuid); this API has no user store of its own.

public class UserProfile
{
    public Guid Id { get; set; } // == auth.users.id
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public int Xp { get; set; }
    public int Level { get; set; }
    public int Streak { get; set; }
    public int DailyXp { get; set; }
    public int WeeklyXp { get; set; }
    public DateTime? LastQuizDate { get; set; }
    public bool IsDisabled { get; set; }
    public Guid? SchoolId { get; set; }
    public string? SchoolRole { get; set; } // 'student' | 'teacher' | 'admin'
    public bool? IsSchoolVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "user"; // 'user' | 'moderator' | 'admin'
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuizAttempt
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public int Score { get; set; }           // percentage 0-100
    public bool Completed { get; set; }
    public string? QuizTitle { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public int? TimeTaken { get; set; }
    public int MaxScore { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public bool? IsVerifiedQuiz { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserTopicProgress
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Status { get; set; } // 'not_started' | 'in_progress' | 'completed'
    public DateTime? LastAttemptedAt { get; set; }
    public int? Score { get; set; }
    public int? Attempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
