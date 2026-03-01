namespace CoMentor.Domain.Entities
{
    public class Classroom
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!; // e.g. "10-A"
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<User> Students { get; set; }
        public ICollection<TeacherClassroom> TeacherClassrooms { get; set; }
        public ICollection<Announcement> Announcements { get; set; }
        public ICollection<Homework> Homeworks { get; set; }

        public Classroom()
        {
            Students = new HashSet<User>();
            TeacherClassrooms = new HashSet<TeacherClassroom>();
            Announcements = new HashSet<Announcement>();
            Homeworks = new HashSet<Homework>();
        }
    }
}
