using System;

namespace CoMentor.Domain.Entities
{
    public class HomeworkSubmission
    {
        public int Id { get; set; }
        
        public int HomeworkId { get; set; }
        public Homework Homework { get; set; } = null!;
        
        public int StudentId { get; set; }
        public User Student { get; set; } = null!;
        
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        
        // İleride öğrencinin yazdığı not, dosya linki veya öğretmenin notu tutulabilir.
        public string? StudentNotes { get; set; }
        public string? TeacherFeedback { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
