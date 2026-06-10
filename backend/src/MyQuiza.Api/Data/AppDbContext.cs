using Microsoft.EntityFrameworkCore;
using MyQuiza.Api.Models;

namespace MyQuiza.Api.Data;

/// <summary>
/// Maps onto EduBridge's EXISTING Supabase (PostgreSQL) schema.
/// This context owns NO migrations — EduBridge's supabase/migrations remain the schema source of truth.
/// Never call EnsureCreated()/Migrate() against this database.
/// Column names resolve to snake_case via EFCore.NamingConventions (see Program.cs).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserTopicProgress> UserTopicProgress => Set<UserTopicProgress>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Subject>(e =>
        {
            e.ToTable("subjects");
            e.HasKey(x => x.Id);
        });

        b.Entity<Chapter>(e =>
        {
            e.ToTable("chapters");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Subject).WithMany(s => s.Chapters)
                .HasForeignKey(x => x.SubjectId).IsRequired(false);
        });

        b.Entity<Topic>(e =>
        {
            e.ToTable("topics");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Chapter).WithMany(c => c.Topics)
                .HasForeignKey(x => x.ChapterId).IsRequired(false);
        });

        b.Entity<Quiz>(e =>
        {
            e.ToTable("quizzes");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Topic).WithMany(t => t.Quizzes)
                .HasForeignKey(x => x.TopicId);
        });

        b.Entity<Question>(e =>
        {
            e.ToTable("questions");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Quiz).WithMany(q => q.Questions)
                .HasForeignKey(x => x.QuizId);
        });

        b.Entity<Answer>(e =>
        {
            e.ToTable("answers");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Question).WithMany(q => q.Answers)
                .HasForeignKey(x => x.QuestionId);
        });

        b.Entity<QuizAttempt>(e =>
        {
            e.ToTable("quiz_attempts");
            e.HasKey(x => x.Id);
        });

        b.Entity<UserProfile>(e =>
        {
            e.ToTable("user_profiles");
            e.HasKey(x => x.Id);
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => x.Id);
        });

        b.Entity<UserTopicProgress>(e =>
        {
            e.ToTable("user_topic_progress");
            e.HasKey(x => x.Id);
        });
    }
}
