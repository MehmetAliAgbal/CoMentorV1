namespace CoMentor.Application.DTOs
{
    public class UserStatsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public int TotalXp { get; set; }
        public int CurrentStreak { get; set; }
        public string? CurrentLeague { get; set; }
        public int TotalTrials { get; set; }
        public double? AvgTrialScore { get; set; }
        public int TotalStudyMinutes { get; set; }
    }
}
