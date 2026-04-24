using System;
using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = null!;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        
        public string RequesterType { get; set; } = null!;
        public string Status { get; set; } = null!; // "Pending", "Scheduled", "Cancelled", "Completed"
        
        public DateTime? AppointmentDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        
        public string? Notes { get; set; }
        public string? TeacherNotes { get; set; }
        public string? MeetingUrl { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }

    public class RequestAppointmentDto
    {
        [Required]
        public int TeacherId { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class ScheduleAppointmentDto
    {
        [Required]
        public DateTime AppointmentDate { get; set; }
        [Required]
        public TimeSpan StartTime { get; set; }
        [Required]
        public TimeSpan EndTime { get; set; }
        
        [MaxLength(500)]
        public string? TeacherNotes { get; set; }
        public string? MeetingUrl { get; set; }
    }
}
