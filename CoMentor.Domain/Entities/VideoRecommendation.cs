namespace CoMentor.Domain.Entities
{
    public class VideoRecommendation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubjectId { get; set; }
        public string Title { get; set; } = null!;
        public string Url { get; set; } = null!;
        public int? DurationMinutes { get; set; }
        public int Priority { get; set; } = 1;
        public string? Reason { get; set; }
        public bool IsWatched { get; set; } = false;
        public DateTime RecommendedAt { get; set; }
        public DateTime? WatchedAt { get; set; }

        public User User { get; set; }
        public Subject Subject { get; set; }
    }
}

