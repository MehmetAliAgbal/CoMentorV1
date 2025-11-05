namespace CoMentor.Domain.Entities
{
    public class TrialExam
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string ExamType { get; set; } = null!; // 'TYT', 'AYT'
        public DateTime ExamDate { get; set; }
        public int TotalScore { get; set; } = 0;
        public int? Ranking { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
        public ICollection<TrialSubjectScore> SubjectScores { get; set; }
    }
}

