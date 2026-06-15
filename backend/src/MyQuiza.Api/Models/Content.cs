namespace MyQuiza.Api.Models;

// Entities mapped to EduBridge's EXISTING Supabase tables (schema owned by EduBridge).
// Column/table names resolve to snake_case via EFCore.NamingConventions.
// This API does NOT own migrations for these tables.

public class Subject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int? OrderIndex { get; set; }
    public string? Category { get; set; }
    public int? CategoryPriority { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Chapter> Chapters { get; set; } = [];
}

public class Chapter
{
    public Guid Id { get; set; }
    public Guid? SubjectId { get; set; }
    public int Form { get; set; }
    public string Name { get; set; } = null!;
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Subject? Subject { get; set; }
    public ICollection<Topic> Topics { get; set; } = [];
}

public class Topic
{
    public Guid Id { get; set; }
    public Guid? ChapterId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? DifficultyLevel { get; set; }
    public int? TimeEstimateMinutes { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Chapter? Chapter { get; set; }
    public ICollection<Quiz> Quizzes { get; set; } = [];
}

public class Quiz
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
    public string Name { get; set; } = null!;
    // NOTE: created_by is `text` in the DB (stores the auth user id as a string).
    public string CreatedBy { get; set; } = null!;
    public bool? Verified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationFeedback { get; set; }
    // NOTE: the quizzes table has NO updated_at column (unlike every other table),
    // so this entity must not declare UpdatedAt — EF would emit a SELECT/INSERT for a
    // column that doesn't exist and every full-entity load/write would 500.
    public DateTime? CreatedAt { get; set; }

    public Topic? Topic { get; set; }
    public ICollection<Question> Questions { get; set; } = [];
}

public class Question
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string Text { get; set; } = null!;
    public string Type { get; set; } = "radio"; // 'radio' | 'checkbox'
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Quiz? Quiz { get; set; }
    public ICollection<Answer> Answers { get; set; } = [];
}

public class Answer
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Question? Question { get; set; }
}
