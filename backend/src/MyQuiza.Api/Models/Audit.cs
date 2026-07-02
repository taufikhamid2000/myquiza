namespace MyQuiza.Api.Models;

// Moderation audit trail — mapped to EduBridge's existing Supabase tables.
// comment_type is DB-constrained to: 'suggestion' | 'issue' | 'approved' | 'rejected'.

public class QuizAuditComment
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid AdminUserId { get; set; }
    public string CommentText { get; set; } = null!;
    public string CommentType { get; set; } = null!;
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Quiz? Quiz { get; set; }
}

public class QuestionAuditComment
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid AdminUserId { get; set; }
    public string CommentText { get; set; } = null!;
    public string CommentType { get; set; } = null!;
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Question? Question { get; set; }
}

public class AnswerAuditComment
{
    public Guid Id { get; set; }
    public Guid AnswerId { get; set; }
    public Guid AdminUserId { get; set; }
    public string CommentText { get; set; } = null!;
    public string CommentType { get; set; } = null!;
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Answer? Answer { get; set; }
}

// action is DB-constrained to: 'verified' | 'unverified' | 'rejected'. Written
// automatically whenever POST /quizzes/{id}/verify runs — never created directly.
public class QuizVerificationLog
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid AdminUserId { get; set; }
    public string Action { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Quiz? Quiz { get; set; }
}
