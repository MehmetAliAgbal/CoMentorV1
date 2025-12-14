using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface ILeagueService
    {
        /// <summary>
        /// Tüm ligleri getirir
        /// </summary>
        Task<List<LeagueDto>> GetAllLeaguesAsync();

        /// <summary>
        /// Belirli bir ligi getirir
        /// </summary>
        Task<LeagueDto?> GetLeagueByIdAsync(int leagueId);

        /// <summary>
        /// XP'ye göre hangi ligde olduğunu belirler
        /// </summary>
        Task<LeagueDto?> GetLeagueByXpAsync(int xp);

        /// <summary>
        /// Kullanıcının mevcut lig durumunu getirir
        /// </summary>
        Task<UserLeagueDto?> GetUserLeagueAsync(int userId);

        /// <summary>
        /// Kullanıcının lig geçmişini getirir
        /// </summary>
        Task<List<UserLeagueHistoryDto>> GetUserLeagueHistoryAsync(int userId);

        /// <summary>
        /// Belirli bir ligteki kullanıcı sıralamasını getirir
        /// </summary>
        Task<LeagueLeaderboardDto?> GetLeagueLeaderboardAsync(int leagueId, int? currentUserId = null, int limit = 100);

        /// <summary>
        /// Tüm liglerin genel görünümünü getirir (kullanıcı sayılarıyla)
        /// </summary>
        Task<AllLeaguesOverviewDto> GetAllLeaguesOverviewAsync(int? currentUserId = null);

        /// <summary>
        /// Kullanıcının ligini kontrol eder ve gerekirse günceller
        /// XP değiştiğinde çağrılmalı
        /// </summary>
        Task<LeagueChangeDto> CheckAndUpdateUserLeagueAsync(int userId);

        /// <summary>
        /// Yeni kullanıcı için başlangıç ligini atar
        /// </summary>
        Task InitializeUserLeagueAsync(int userId);
    }
}

