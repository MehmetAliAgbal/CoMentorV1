namespace CoMentor.Domain.Entities
{
    public class TeacherClassroom
    {
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public int ClassroomId { get; set; }
        public Classroom Classroom { get; set; } = null!;
    }
}
