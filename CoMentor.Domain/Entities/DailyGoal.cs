namespace CoMentor.Domain.Entities
{
    public class DailyGoal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateOnly GoalDate { get; set; }
        public int TargetStudyMinutes { get; set; }
        public int ActualStudyMinutes { get; set; } = 0;
        public int TargetSubjects { get; set; } = 3;
        public int CompletedSubjects { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public int CompletionPercentage => TargetStudyMinutes > 0
            ? Math.Min(100, (ActualStudyMinutes * 100) / TargetStudyMinutes)
            : 0;

        public User User { get; set; }
    }
}
