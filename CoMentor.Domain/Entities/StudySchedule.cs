namespace CoMentor.Domain.Entities
{
    public class StudySchedule
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubjectId { get; set; }
        public int DayOfWeek { get; set; } // 0=Pazar
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? Topic { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
        public Subject Subject { get; set; }
    }
}

