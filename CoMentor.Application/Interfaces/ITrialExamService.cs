using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface ITrialExamService
    {
        /// <summary>
        /// Yeni deneme kaydı oluşturur
        /// </summary>
        Task<TrialExamDto?> CreateTrialExamAsync(int userId, CreateTrialExamRequest request);

        /// <summary>
        /// Deneme bilgilerini günceller
        /// </summary>
        Task<TrialExamDto?> UpdateTrialExamAsync(int userId, int trialId, UpdateTrialExamRequest request);

        /// <summary>
        /// Deneme kaydını siler
        /// </summary>
        Task<bool> DeleteTrialExamAsync(int userId, int trialId);

        /// <summary>
        /// Belirli bir denemenin detayını getirir
        /// </summary>
        Task<TrialExamDto?> GetTrialExamByIdAsync(int userId, int trialId);

        /// <summary>
        /// Kullanıcının tüm denemelerini istatistiklerle birlikte getirir
        /// </summary>
        Task<TrialExamListResponse> GetTrialExamsAsync(int userId, string? examType = null);

        /// <summary>
        /// Kullanıcının deneme istatistiklerini getirir
        /// </summary>
        Task<TrialExamStatsDto> GetTrialExamStatsAsync(int userId, string? examType = null);
    }
}

