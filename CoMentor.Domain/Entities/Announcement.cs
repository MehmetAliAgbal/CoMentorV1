namespace CoMentor.Domain.Entities
{
    public class Announcement
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Hedef Kitle (Sınıf geneli veya tekil öğrenci)
        public int? ClassroomId { get; set; }
        public Classroom? Classroom { get; set; }

        public int? UserId { get; set; } // Öğrenci
        public User? User { get; set; }

        public int? ParentId { get; set; } // Veli (Bireysel)
        public Parent? Parent { get; set; }

        // "Students", "Parents", "Both" (Toplu mesajlar veya genel gosterim için kitle)
        public string TargetAudience { get; set; } = "Students";
    }
}
