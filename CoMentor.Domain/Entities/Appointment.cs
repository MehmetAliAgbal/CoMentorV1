using System;

namespace CoMentor.Domain.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        
        // Hangi öğretmenle randevu yapılacak
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;
        
        // Hangi öğrenci üzerinden randevu yapılıyor
        public int StudentId { get; set; }
        public User Student { get; set; } = null!;
        
        // Talebin Sahibi (Requester: "Student", "Parent", "Teacher")
        public string RequesterType { get; set; } = "Student";
        
        // "Pending", "Scheduled", "Cancelled", "Completed"
        public string Status { get; set; } = "Pending";
        
        // Randevu ayarlanana kadar Null olabilir
        public DateTime? AppointmentDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        
        // Talebi atanın notu
        public string? Notes { get; set; }
        // Öğretmenin onaylarken yazdığı not
        public string? TeacherNotes { get; set; }
        
        // Online görüşme için (opsiyonel)
        public string? MeetingUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
