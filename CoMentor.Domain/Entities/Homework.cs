namespace CoMentor.Domain.Entities
{
    public class Homework
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public string Subject { get; set; } = null!; // Ders (Örn: Matematik)
        public string Topic { get; set; } = null!; // Konu / Başlık
        public string? Description { get; set; }
        public DateTime DueDate { get; set; } // Son Teslim Tarihi
        public DateTime CreatedAt { get; set; }

        // Hedef Kitle (Sınıf geneli veya tekil öğrenci)
        public int? ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }

        public int? UserId { get; set; } // Öğrenci
        public User? User { get; set; }
    }
}
