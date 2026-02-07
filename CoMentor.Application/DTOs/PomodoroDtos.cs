using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs
{
    #region Pomodoro DTOs

    /// <summary>
    /// Pomodoro başlatma isteği
    /// </summary>
    public class StartPomodoroRequest
    {
        public int? SubjectId { get; set; }

        public int? ScheduleId { get; set; }

        [Required]
        [Range(1, 120)]
        public int PlannedDurationMinutes { get; set; } = 25;

        [RegularExpression("^(STUDY|SHORT_BREAK|LONG_BREAK)$")]
        public string SessionType { get; set; } = "STUDY";

        public string? Notes { get; set; }
    }

    /// <summary>
    /// Pomodoro tamamlama isteği
    /// </summary>
    public class CompletePomodoroRequest
    {
        public int? ActualDurationMinutes { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Pomodoro oturumu DTO
    /// </summary>
    public class PomodoroSessionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int? ScheduleId { get; set; }
        public string SessionType { get; set; } = null!;
        public int PlannedDurationMinutes { get; set; }
        public int? ActualDurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public string? Notes { get; set; }
        public int XpEarned { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Pomodoro istatistikleri
    /// </summary>
    public class PomodoroStatsDto
    {
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int TotalMinutesStudied { get; set; }
        public int TotalXpEarned { get; set; }
        public int TodayMinutes { get; set; }
        public int TodaySessions { get; set; }
        public int WeekMinutes { get; set; }
        public double AverageSessionMinutes { get; set; }
    }

    #endregion

}

