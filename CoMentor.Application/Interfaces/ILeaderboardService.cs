using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface ILeaderboardService
    {
        #region Leaderboard

        /// <summary>
        /// Genel XP sıralamasını getirir
        /// </summary>
        Task<LeaderboardResponse> GetGeneralLeaderboardAsync(int? currentUserId = null, int limit = 100);

        /// <summary>
        /// Okul ligini getirir (aynı okuldaki kullanıcılar)
        /// </summary>
        Task<LeaderboardResponse> GetSchoolLeaderboardAsync(int userId, int limit = 100);

        /// <summary>
        /// Sınıf ligini getirir (aynı sınıftaki kullanıcılar)
        /// </summary>
        Task<LeaderboardResponse> GetGradeLeaderboardAsync(int userId, int limit = 100);

        /// <summary>
        /// Tüm ligleri tek seferde getirir (Genel, Okul, Sınıf)
        /// </summary>
        Task<AllLeaguesResponse> GetAllLeaguesAsync(int userId, int limit = 50);

        #endregion

        #region XP

        /// <summary>
        /// Kullanıcının XP geçmişini getirir
        /// </summary>
        Task<List<XpTransactionDto>> GetXpHistoryAsync(int userId, int limit = 50);

        /// <summary>
        /// Kullanıcının XP özetini getirir (toplam, bugün, hafta, sıralamalar)
        /// </summary>
        Task<UserXpSummaryDto> GetUserXpSummaryAsync(int userId);

        #endregion
    }
}

