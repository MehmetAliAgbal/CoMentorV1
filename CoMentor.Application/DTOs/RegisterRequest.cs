namespace CoMentor.Application.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? SchoolName { get; set; }
        public int? GradeLevel { get; set; }
        public string? TargetExam { get; set; } // 'TYT', 'AYT', 'BOTH'
        public int? DailyGoalMinutes { get; set; }
    }
}