namespace CoMentor.Domain.Entities
{
    public class League
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int MinXp { get; set; }
        public int? MaxXp { get; set; }
        public string? LeagueColor { get; set; }
        public string? Icon { get; set; }
        public int RankOrder { get; set; }

        public ICollection<UserLeagueHistory> UserHistories { get; set; }
    }
}

