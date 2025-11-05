namespace CoMentor.Domain.Entities
{
    public class PomodoroSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? SubjectId { get; set; }
        public int? ScheduleId { get; set; }
        public string SessionType { get; set; } = "STUDY"; // 'STUDY', 'SHORT_BREAK', 'LONG_BREAK'
        public int PlannedDurationMinutes { get; set; }
        public int? ActualDurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; } = false;
        public string? Notes { get; set; }
        public int XpEarned { get; set; } = 0;
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
        public Subject? Subject { get; set; }
        public StudySchedule? Schedule { get; set; }
    }
}

