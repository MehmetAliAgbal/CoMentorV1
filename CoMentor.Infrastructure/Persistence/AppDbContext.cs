using Microsoft.EntityFrameworkCore;
using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs; // UserStatsDto için

namespace CoMentor.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Tablolar (Entities)
        public DbSet<User> Users { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TrialExam> TrialExams { get; set; }
        public DbSet<TrialSubjectScore> TrialSubjectScores { get; set; }
        public DbSet<StudySchedule> StudySchedules { get; set; }
        public DbSet<PomodoroSession> PomodoroSessions { get; set; }
        public DbSet<XpTransaction> XpTransactions { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<League> Leagues { get; set; }
        public DbSet<UserLeagueHistory> UserLeagueHistories { get; set; }
        public DbSet<DailyGoal> DailyGoals { get; set; }
        public DbSet<StudyStreak> StudyStreaks { get; set; }
        public DbSet<VideoRecommendation> VideoRecommendations { get; set; }

        // SQL VIEW (readonly)
        public DbSet<UserStatsDto> UserStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQL View - ReadOnly DTO
            modelBuilder.Entity<UserStatsDto>().HasNoKey().ToView("user_stats");

            // Diğer entity konfigurasyonları için Fluent API yazılabilir (opsiyonel)
        }
    }
}
