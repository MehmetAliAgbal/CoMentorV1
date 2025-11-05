namespace CoMentor.Domain.Entities
{
    public class TrialSubjectScore
    {
        public int Id { get; set; }
        public int TrialExamId { get; set; }
        public int SubjectId { get; set; }
        public int CorrectAnswers { get; set; } = 0;
        public int WrongAnswers { get; set; } = 0;
        public int EmptyAnswers { get; set; } = 0;
        public double NetScore => CorrectAnswers - (WrongAnswers / 4.0);
        public int? TimeSpentMinutes { get; set; }

        public TrialExam TrialExam { get; set; }
        public Subject Subject { get; set; }
    }
}

