namespace CoMentor.Domain.Entities
{
    public class StudyStreak
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int CurrentDays { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        public User User { get; set; }
    }
}

