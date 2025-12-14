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

            // League Seed Data - XP Seviye Sistemi
            modelBuilder.Entity<League>().HasData(
                new League
                {
                    Id = 1,
                    Name = "Bronz",
                    MinXp = 0,
                    MaxXp = 999,
                    LeagueColor = "#CD7F32",
                    Icon = "🥉",
                    RankOrder = 1
                },
                new League
                {
                    Id = 2,
                    Name = "Gümüş",
                    MinXp = 1000,
                    MaxXp = 4999,
                    LeagueColor = "#C0C0C0",
                    Icon = "🥈",
                    RankOrder = 2
                },
                new League
                {
                    Id = 3,
                    Name = "Altın",
                    MinXp = 5000,
                    MaxXp = 14999,
                    LeagueColor = "#FFD700",
                    Icon = "🥇",
                    RankOrder = 3
                },
                new League
                {
                    Id = 4,
                    Name = "Platin",
                    MinXp = 15000,
                    MaxXp = 49999,
                    LeagueColor = "#E5E4E2",
                    Icon = "💎",
                    RankOrder = 4
                },
                new League
                {
                    Id = 5,
                    Name = "Elmas",
                    MinXp = 50000,
                    MaxXp = null, // Üst limit yok
                    LeagueColor = "#B9F2FF",
                    Icon = "👑",
                    RankOrder = 5
                }
            );

        }
    }
}
