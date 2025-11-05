namespace CoMentor.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? SchoolName { get; set; }
        public int? GradeLevel { get; set; }
        public string? TargetExam { get; set; } // 'TYT', 'AYT', 'BOTH'
        public int CurrentStreak { get; set; } = 0;
        public int TotalXp { get; set; } = 0;
        public int DailyGoalMinutes { get; set; } = 120;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<TrialExam> TrialExams { get; set; }
        public ICollection<StudySchedule> StudySchedules { get; set; }
        public ICollection<PomodoroSession> PomodoroSessions { get; set; }
        public ICollection<XpTransaction> XpTransactions { get; set; }
        public ICollection<UserAchievement> UserAchievements { get; set; }
        public ICollection<UserLeagueHistory> LeagueHistories { get; set; }
        public ICollection<DailyGoal> DailyGoals { get; set; }
        public ICollection<StudyStreak> StudyStreaks { get; set; }
        public ICollection<VideoRecommendation> VideoRecommendations { get; set; }
    }
}

