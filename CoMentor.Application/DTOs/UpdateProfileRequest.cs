namespace CoMentor.Application.DTOs
{
    public class UpdateProfileRequest
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? AvatarUrl { get; set; }
        public string? SchoolName { get; set; }
        public int? GradeLevel { get; set; }
        public string? TargetExam { get; set; }
        public int? DailyGoalMinutes { get; set; }
    }
}
