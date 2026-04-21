using System;

namespace CoMentor.Application.DTOs
{
    public class VideoRecommendationDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Url { get; set; } = null!;
        public DateTime RecommendedAt { get; set; }
        public bool IsWatched { get; set; }
    }
}
