using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces;

public interface ITeacherPanelService
{
    // Classrooms
    Task<List<ClassroomDto>> GetTeacherClassroomsAsync(int teacherId);
    Task<ClassroomDto> CreateClassroomAsync(int teacherId, CreateClassroomRequest request);
    Task<bool> AddStudentToClassroomAsync(int teacherId, int classroomId, int studentId);
    Task<bool> RemoveStudentFromClassroomAsync(int teacherId, int classroomId, int studentId);

    // Announcements
    Task<List<AnnouncementDto>> GetTeacherAnnouncementsAsync(int teacherId);
    Task<AnnouncementDto> CreateAnnouncementAsync(int teacherId, CreateAnnouncementRequest request);
    
    // Homeworks
    Task<List<HomeworkDto>> GetTeacherHomeworksAsync(int teacherId);
    Task<HomeworkDto> CreateHomeworkAsync(int teacherId, CreateHomeworkRequest request);
    
    // Student Dashboard endpoints
    Task<List<AnnouncementDto>> GetStudentAnnouncementsAsync(int studentId);
    Task<List<HomeworkDto>> GetStudentHomeworksAsync(int studentId);
    
    // Student Monitoring (Trial Exams)
    Task<List<StudentPerformanceDto>> GetClassroomStudentPerformancesAsync(int teacherId, int classroomId);
    Task<TrialExamDto?> AddStudentTrialExamAsync(int teacherId, int studentId, CreateTrialExamRequest request);
    Task<TrialExamListResponse?> GetStudentTrialExamsAsync(int teacherId, int studentId, string? examType = null);
    Task<TrialExamDto?> GetStudentTrialExamDetailAsync(int teacherId, int studentId, int trialId);
}

public class StudentPerformanceDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public int TotalTrialExams { get; set; }
    public double AverageNetScore { get; set; }
    public string? TargetExam { get; set; }
}
