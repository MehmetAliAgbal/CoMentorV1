namespace CoMentor.Domain.Entities
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? ColorHex { get; set; }
        public string? ExamType { get; set; } // 'TYT', 'AYT', 'BOTH'
        public int? MaxQuestions { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<TrialSubjectScore> TrialSubjectScores { get; set; }
        public ICollection<StudySchedule> StudySchedules { get; set; }
        public ICollection<PomodoroSession> PomodoroSessions { get; set; }
        public ICollection<VideoRecommendation> VideoRecommendations { get; set; }
    }
}

