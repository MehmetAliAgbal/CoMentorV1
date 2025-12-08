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

    #region Study Schedule DTOs

    /// <summary>
    /// Ders programı ekleme isteği
    /// </summary>
    public class CreateScheduleRequest
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        [Range(0, 6)]
        public int DayOfWeek { get; set; } // 0=Pazar, 1=Pazartesi...

        [Required]
        public string StartTime { get; set; } = null!; // "09:00" formatında

        [Required]
        public string EndTime { get; set; } = null!; // "10:30" formatında

        public string? Topic { get; set; }
    }

    /// <summary>
    /// Ders programı güncelleme isteği
    /// </summary>
    public class UpdateScheduleRequest
    {
        public int? SubjectId { get; set; }
        public int? DayOfWeek { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Topic { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Ders programı DTO
    /// </summary>
    public class StudyScheduleDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = null!;
        public string SubjectShortName { get; set; } = null!;
        public string? SubjectColorHex { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = null!;
        public string StartTime { get; set; } = null!;
        public string EndTime { get; set; } = null!;
        public int DurationMinutes { get; set; }
        public string? Topic { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Günlük program özeti
    /// </summary>
    public class DailyScheduleDto
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = null!;
        public List<StudyScheduleDto> Schedules { get; set; } = new();
        public int TotalMinutes { get; set; }
    }

    #endregion
}

