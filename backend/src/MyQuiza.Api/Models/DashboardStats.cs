namespace MyQuiza.Api.Models;

// Read-only materialized view — keyless entity, mapped via ToView so EF never
// attempts inserts/updates/deletes against it. Refresh cadence is owned by EduBridge.
public class UserDashboardStats
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public int Streak { get; set; }
    public int Xp { get; set; }
    public int Level { get; set; }
    public DateTime? LastQuizDate { get; set; }
    public long CompletedQuizzes { get; set; }
    public decimal AverageScore { get; set; }
    public long ActiveDays { get; set; }
    public long WeeklyQuizzes { get; set; }
    public decimal WeeklyAverageScore { get; set; }
}
