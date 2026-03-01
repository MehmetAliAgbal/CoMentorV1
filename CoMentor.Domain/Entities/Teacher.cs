namespace CoMentor.Domain.Entities
{
    public class Teacher
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? Branch { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<TeacherClassroom> TeacherClassrooms { get; set; }
        public ICollection<Announcement> Announcements { get; set; }
        public ICollection<Homework> Homeworks { get; set; }

        public Teacher()
        {
            TeacherClassrooms = new HashSet<TeacherClassroom>();
            Announcements = new HashSet<Announcement>();
            Homeworks = new HashSet<Homework>();
        }
    }
}
