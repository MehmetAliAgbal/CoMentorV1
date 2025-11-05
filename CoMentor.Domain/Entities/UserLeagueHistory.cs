namespace CoMentor.Domain.Entities
{
    public class UserLeagueHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int LeagueId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? FinalXp { get; set; }
        public int? FinalRank { get; set; }
        public bool IsCurrent { get; set; } = false;

        public User User { get; set; }
        public League League { get; set; }
    }
}

