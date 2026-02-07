using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs
{
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
}
