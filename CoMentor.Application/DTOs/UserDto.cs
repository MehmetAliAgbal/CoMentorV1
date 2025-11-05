namespace CoMentor.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? SchoolName { get; set; }
        public int? GradeLevel { get; set; }
        public string? TargetExam { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalXp { get; set; }
        public int DailyGoalMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
