namespace CoMentor.Domain.Entities
{
    public class XpTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Amount { get; set; }
        public string SourceType { get; set; } = null!; // 'POMODORO', 'TRIAL_EXAM', ...
        public int? SourceId { get; set; }
        public string? Description { get; set; }
        public DateTime EarnedAt { get; set; }

        public User User { get; set; }
    }
}

