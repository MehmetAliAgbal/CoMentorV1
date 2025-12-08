namespace CoMentor.Application.DTOs
{
    #region Leaderboard DTOs

    /// <summary>
    /// Lig türleri
    /// </summary>
    public enum LeagueType
    {
        General,    // Genel sıralama (tüm kullanıcılar)
        School,     // Okul ligi (aynı okuldaki kullanıcılar)
        Grade       // Sınıf ligi (aynı sınıftaki kullanıcılar)
    }

    /// <summary>
    /// Sıralama listesi item
    /// </summary>
    public class LeaderboardItemDto
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? SchoolName { get; set; }
        public int? GradeLevel { get; set; }
        public int TotalXp { get; set; }
        public int CurrentStreak { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    /// <summary>
    /// Sıralama listesi response
    /// </summary>
    public class LeaderboardResponse
    {
        public string LeagueType { get; set; } = "General";
        public string? LeagueName { get; set; }
        public List<LeaderboardItemDto> Rankings { get; set; } = new();
        public LeaderboardItemDto? CurrentUserRank { get; set; }
        public int TotalUsers { get; set; }
    }

    /// <summary>
    /// Tüm liglerin özet bilgisi
    /// </summary>
    public class AllLeaguesResponse
    {
        public LeaderboardResponse General { get; set; } = new();
        public LeaderboardResponse? School { get; set; }
        public LeaderboardResponse? Grade { get; set; }
    }

    #endregion

    #region XP DTOs

    /// <summary>
    /// XP işlem geçmişi DTO
    /// </summary>
    public class XpTransactionDto
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public string SourceType { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime EarnedAt { get; set; }
    }

    /// <summary>
    /// Kullanıcı XP özeti
    /// </summary>
    public class UserXpSummaryDto
    {
        public int UserId { get; set; }
        public int TotalXp { get; set; }
        public int TodayXp { get; set; }
        public int WeekXp { get; set; }
        public int MonthXp { get; set; }
        public int CurrentStreak { get; set; }
        public GeneralRankDto GeneralRank { get; set; } = new();
        public SchoolRankDto? SchoolRank { get; set; }
        public GradeRankDto? GradeRank { get; set; }
    }

    public class GeneralRankDto
    {
        public int Rank { get; set; }
        public int TotalUsers { get; set; }
    }

    public class SchoolRankDto
    {
        public int Rank { get; set; }
        public int TotalUsers { get; set; }
        public string SchoolName { get; set; } = null!;
    }

    public class GradeRankDto
    {
        public int Rank { get; set; }
        public int TotalUsers { get; set; }
        public int GradeLevel { get; set; }
        public string GradeName { get; set; } = null!;
    }

    #endregion
}

