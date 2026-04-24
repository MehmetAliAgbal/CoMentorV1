namespace CoMentor.Domain.Entities
{
    public class Parent
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;

        // One-to-One relationship with User (Student)
        public int StudentId { get; set; }
        public User Student { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
