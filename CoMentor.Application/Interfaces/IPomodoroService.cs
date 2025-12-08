using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface IPomodoroService
    {
        #region Pomodoro Session

        /// <summary>
        /// Yeni pomodoro oturumu başlatır
        /// </summary>
        Task<PomodoroSessionDto?> StartPomodoroAsync(int userId, StartPomodoroRequest request);

        /// <summary>
        /// Pomodoro oturumunu tamamlar ve XP kazandırır
        /// </summary>
        Task<PomodoroSessionDto?> CompletePomodoroAsync(int userId, int sessionId, CompletePomodoroRequest? request = null);

        /// <summary>
        /// Pomodoro oturumunu iptal eder
        /// </summary>
        Task<bool> CancelPomodoroAsync(int userId, int sessionId);

        /// <summary>
        /// Aktif pomodoro oturumunu getirir
        /// </summary>
        Task<PomodoroSessionDto?> GetActivePomodoroAsync(int userId);

        /// <summary>
        /// Kullanıcının pomodoro geçmişini getirir
        /// </summary>
        Task<List<PomodoroSessionDto>> GetPomodoroHistoryAsync(int userId, int? subjectId = null, int limit = 50);

        /// <summary>
        /// Pomodoro istatistiklerini getirir
        /// </summary>
        Task<PomodoroStatsDto> GetPomodoroStatsAsync(int userId);

        #endregion

        #region Study Schedule

        /// <summary>
        /// Ders programı ekler
        /// </summary>
        Task<StudyScheduleDto?> CreateScheduleAsync(int userId, CreateScheduleRequest request);

        /// <summary>
        /// Ders programını günceller
        /// </summary>
        Task<StudyScheduleDto?> UpdateScheduleAsync(int userId, int scheduleId, UpdateScheduleRequest request);

        /// <summary>
        /// Ders programını siler
        /// </summary>
        Task<bool> DeleteScheduleAsync(int userId, int scheduleId);

        /// <summary>
        /// Kullanıcının tüm ders programını getirir
        /// </summary>
        Task<List<DailyScheduleDto>> GetWeeklyScheduleAsync(int userId);

        /// <summary>
        /// Bugünün ders programını getirir
        /// </summary>
        Task<DailyScheduleDto> GetTodayScheduleAsync(int userId);

        #endregion
    }
}

