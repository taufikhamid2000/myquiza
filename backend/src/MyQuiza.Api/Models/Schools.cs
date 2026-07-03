namespace MyQuiza.Api.Models;

public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    // DB-constrained: SMK | SMKA | MRSM | Sekolah Sains | Sekolah Sukan | Sekolah Seni | SBP | SMJK | KV
    public string Type { get; set; } = null!;
    public string? Code { get; set; }
    public string District { get; set; } = null!;
    public string State { get; set; } = null!;
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? PrincipalName { get; set; }
    public int? TotalStudents { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SchoolStats? Stats { get; set; }
}

// 1:1 with School via SchoolId as the primary key (no own id column).
public class SchoolStats
{
    public Guid SchoolId { get; set; }
    public decimal AverageScore { get; set; }
    public decimal ParticipationRate { get; set; }
    public int TotalQuizzesTaken { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int CorrectAnswers { get; set; }
    public int ActiveStudents { get; set; }
    public DateTime LastCalculatedAt { get; set; }

    public School? School { get; set; }
}
