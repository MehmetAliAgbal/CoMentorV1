using Microsoft.EntityFrameworkCore;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Domain.Entities;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services;

public class TeacherPanelService : ITeacherPanelService
{
    private readonly AppDbContext _db;
    private readonly ITrialExamService _trialExamService;

    public TeacherPanelService(AppDbContext db, ITrialExamService trialExamService)
    {
        _db = db;
        _trialExamService = trialExamService;
    }

    #region Classrooms

    public async Task<List<ClassroomDto>> GetTeacherClassroomsAsync(int teacherId)
    {
        return await _db.TeacherClassrooms
            .Where(tc => tc.TeacherId == teacherId)
            .Include(tc => tc.Classroom)
            .ThenInclude(c => c.Students)
            .Select(tc => new ClassroomDto
            {
                Id = tc.Classroom.Id,
                Name = tc.Classroom.Name,
                Description = tc.Classroom.Description,
                StudentCount = tc.Classroom.Students.Count
            })
            .ToListAsync();
    }

    public async Task<ClassroomDto> CreateClassroomAsync(int teacherId, CreateClassroomRequest request)
    {
        var classroom = new Classroom
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Classrooms.Add(classroom);
        await _db.SaveChangesAsync(); // To get the generated ClassroomId

        var teacherClassroom = new TeacherClassroom
        {
            TeacherId = teacherId,
            ClassroomId = classroom.Id
        };
        _db.TeacherClassrooms.Add(teacherClassroom);
        await _db.SaveChangesAsync();

        return new ClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            StudentCount = 0
        };
    }

    public async Task<bool> AddStudentToClassroomAsync(int teacherId, int classroomId, int studentId)
    {
        // Yetki kontrolü: Sınıf bu öğretmene ait mi?
        var hasAccess = await _db.TeacherClassrooms.AnyAsync(tc => tc.TeacherId == teacherId && tc.ClassroomId == classroomId);
        if (!hasAccess) return false;

        var student = await _db.Users.FindAsync(studentId);
        if (student == null) return false;

        student.ClassroomId = classroomId;
        student.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveStudentFromClassroomAsync(int teacherId, int classroomId, int studentId)
    {
         // Yetki kontrolü: Sınıf bu öğretmene ait mi?
        var hasAccess = await _db.TeacherClassrooms.AnyAsync(tc => tc.TeacherId == teacherId && tc.ClassroomId == classroomId);
        if (!hasAccess) return false;

        var student = await _db.Users.FindAsync(studentId);
        if (student == null || student.ClassroomId != classroomId) return false;

        student.ClassroomId = null;
        student.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Announcements

    public async Task<List<AnnouncementDto>> GetTeacherAnnouncementsAsync(int teacherId)
    {
        return await _db.Announcements
            .Where(a => a.TeacherId == teacherId)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                TeacherId = a.TeacherId,
                TeacherName = $"{a.Teacher.Name} {a.Teacher.Surname}",
                Title = a.Title,
                Message = a.Message,
                CreatedAt = a.CreatedAt,
                ClassroomId = a.ClassroomId,
                ClassroomName = a.Classroom != null ? a.Classroom.Name : null,
                UserId = a.UserId,
                UserName = a.User != null ? $"{a.User.Name} {a.User.Surname}" : null
            })
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(int teacherId, CreateAnnouncementRequest request)
    {
        var teacher = await _db.Teachers.FindAsync(teacherId);
        if (teacher == null) throw new InvalidOperationException("Teacher not found");

        var announcement = new Announcement
        {
            TeacherId = teacherId,
            Title = request.Title,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow,
            ClassroomId = request.ClassroomId,
            UserId = request.UserId
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();

        var classroom = request.ClassroomId.HasValue ? await _db.Classrooms.FindAsync(request.ClassroomId.Value) : null;
        var user = request.UserId.HasValue ? await _db.Users.FindAsync(request.UserId.Value) : null;

        return new AnnouncementDto
        {
            Id = announcement.Id,
            TeacherId = teacherId,
            TeacherName = $"{teacher.Name} {teacher.Surname}",
            Title = announcement.Title,
            Message = announcement.Message,
            CreatedAt = announcement.CreatedAt,
            ClassroomId = announcement.ClassroomId,
            ClassroomName = classroom?.Name,
            UserId = announcement.UserId,
            UserName = user != null ? $"{user.Name} {user.Surname}" : null
        };
    }

    public async Task<List<AnnouncementDto>> GetStudentAnnouncementsAsync(int studentId)
    {
        var student = await _db.Users.FindAsync(studentId);
        if (student == null) return new List<AnnouncementDto>();

        return await _db.Announcements
            .Where(a => a.UserId == studentId || (student.ClassroomId != null && a.ClassroomId == student.ClassroomId))
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                TeacherId = a.TeacherId,
                TeacherName = $"{a.Teacher.Name} {a.Teacher.Surname}",
                Title = a.Title,
                Message = a.Message,
                CreatedAt = a.CreatedAt,
                ClassroomId = a.ClassroomId,
                ClassroomName = a.Classroom != null ? a.Classroom.Name : null,
                UserId = a.UserId,
                UserName = a.User != null ? $"{a.User.Name} {a.User.Surname}" : null
            })
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Homeworks

    public async Task<List<HomeworkDto>> GetTeacherHomeworksAsync(int teacherId)
    {
        return await _db.Homeworks
            .Where(h => h.TeacherId == teacherId)
            .Select(h => new HomeworkDto
            {
                Id = h.Id,
                TeacherId = h.TeacherId,
                TeacherName = $"{h.Teacher.Name} {h.Teacher.Surname}",
                Subject = h.Subject,
                Topic = h.Topic,
                Description = h.Description,
                DueDate = h.DueDate,
                CreatedAt = h.CreatedAt,
                ClassroomId = h.ClassroomId,
                ClassroomName = h.Classroom != null ? h.Classroom.Name : null,
                UserId = h.UserId,
                UserName = h.User != null ? $"{h.User.Name} {h.User.Surname}" : null
            })
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<HomeworkDto> CreateHomeworkAsync(int teacherId, CreateHomeworkRequest request)
    {
        var teacher = await _db.Teachers.FindAsync(teacherId);
        if (teacher == null) throw new InvalidOperationException("Teacher not found");

        var homework = new Homework
        {
            TeacherId = teacherId,
            Subject = request.Subject,
            Topic = request.Topic,
            Description = request.Description,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
            ClassroomId = request.ClassroomId,
            UserId = request.UserId
        };

        _db.Homeworks.Add(homework);
        await _db.SaveChangesAsync();
        
        var classroom = request.ClassroomId.HasValue ? await _db.Classrooms.FindAsync(request.ClassroomId.Value) : null;
        var user = request.UserId.HasValue ? await _db.Users.FindAsync(request.UserId.Value) : null;

        return new HomeworkDto
        {
            Id = homework.Id,
            TeacherId = teacherId,
            TeacherName = $"{teacher.Name} {teacher.Surname}",
            Subject = homework.Subject,
            Topic = homework.Topic,
            Description = homework.Description,
            DueDate = homework.DueDate,
            CreatedAt = homework.CreatedAt,
            ClassroomId = homework.ClassroomId,
            ClassroomName = classroom?.Name,
            UserId = homework.UserId,
            UserName = user != null ? $"{user.Name} {user.Surname}" : null
        };
    }

    public async Task<List<HomeworkDto>> GetStudentHomeworksAsync(int studentId)
    {
        var student = await _db.Users.FindAsync(studentId);
        if (student == null) return new List<HomeworkDto>();

        return await _db.Homeworks
            .Where(h => h.UserId == studentId || (student.ClassroomId != null && h.ClassroomId == student.ClassroomId))
            .Select(h => new HomeworkDto
            {
                Id = h.Id,
                TeacherId = h.TeacherId,
                TeacherName = $"{h.Teacher.Name} {h.Teacher.Surname}",
                Subject = h.Subject,
                Topic = h.Topic,
                Description = h.Description,
                DueDate = h.DueDate,
                CreatedAt = h.CreatedAt,
                ClassroomId = h.ClassroomId,
                ClassroomName = h.Classroom != null ? h.Classroom.Name : null,
                UserId = h.UserId,
                UserName = h.User != null ? $"{h.User.Name} {h.User.Surname}" : null
            })
            .OrderByDescending(h => h.DueDate)
            .ToListAsync();
    }

    #endregion

    #region Student Monitoring

    public async Task<List<StudentPerformanceDto>> GetClassroomStudentPerformancesAsync(int teacherId, int classroomId)
    {
        // Yetki kontrolü: Sınıf bu öğretmene ait mi?
        var hasAccess = await _db.TeacherClassrooms.AnyAsync(tc => tc.TeacherId == teacherId && tc.ClassroomId == classroomId);
        if (!hasAccess) return new List<StudentPerformanceDto>();

        var users = await _db.Users
            .Where(u => u.ClassroomId == classroomId)
            .Include(u => u.TrialExams)
                .ThenInclude(te => te.SubjectScores)
            .ToListAsync();

        return users
            .Select(u =>
            {
                var totalTrialExams = u.TrialExams?.Count ?? 0;

                double averageNetScore = 0;
                if (u.TrialExams != null && u.TrialExams.Any())
                {
                    var examNetScores = u.TrialExams.Select(te =>
                        te.SubjectScores != null && te.SubjectScores.Any()
                            ? te.SubjectScores.Sum(ss => ss.NetScore)
                            : 0
                    );

                    if (examNetScores.Any())
                    {
                        averageNetScore = examNetScores.Average();
                    }
                }

                return new StudentPerformanceDto
                {
                    StudentId = u.Id,
                    StudentName = $"{u.Name} {u.Surname}",
                    AvatarUrl = u.AvatarUrl,
                    TargetExam = u.TargetExam,
                    TotalTrialExams = totalTrialExams,
                    AverageNetScore = averageNetScore
                };
            })
            .OrderByDescending(sp => sp.AverageNetScore)
            .ToList();
    }

    public async Task<TrialExamDto?> AddStudentTrialExamAsync(int teacherId, int studentId, CreateTrialExamRequest request)
    {
        // Yetki kontrolü: Öğrenci öğretmenin bir sınıfında mı?
        var student = await _db.Users.FindAsync(studentId);
        if (student == null || student.ClassroomId == null) return null;

        var hasAccess = await _db.TeacherClassrooms.AnyAsync(tc => tc.TeacherId == teacherId && tc.ClassroomId == student.ClassroomId);
        if (!hasAccess) throw new UnauthorizedAccessException("Bu öğrenciye deneme ekleme yetkiniz yok.");

        return await _trialExamService.CreateTrialExamAsync(studentId, request);
    }

    public async Task<TrialExamListResponse?> GetStudentTrialExamsAsync(int teacherId, int studentId, string? examType = null)
    {
        // Yetki kontrolü: Öğrenci öğretmenin bir sınıfında mı?
        var student = await _db.Users.FindAsync(studentId);
        if (student == null || student.ClassroomId == null) return null;

        var hasAccess = await _db.TeacherClassrooms.AnyAsync(tc => tc.TeacherId == teacherId && tc.ClassroomId == student.ClassroomId);
        if (!hasAccess) throw new UnauthorizedAccessException("Bu öğrencinin denemelerini görme yetkiniz yok.");

        // ITrialExamService üzerinden öğrencinin denemelerini getir
        return await _trialExamService.GetTrialExamsAsync(studentId, examType);
    }

    public async Task<TrialExamDto?> GetStudentTrialExamDetailAsync(int teacherId, int studentId, int trialId)
    {
        // Yetki kontrolü: Öğrenci öğretmenin bir sınıfında mı?
        var student = await _db.Users.FindAsync(studentId);
        if (student == null || student.ClassroomId == null) return null;

        var hasAccess = await _db.TeacherClassrooms.AnyAsync(tc => tc.TeacherId == teacherId && tc.ClassroomId == student.ClassroomId);
        if (!hasAccess) throw new UnauthorizedAccessException("Bu öğrencinin deneme detayını görme yetkiniz yok.");

        return await _trialExamService.GetTrialExamByIdAsync(studentId, trialId);
    }

    #endregion
}
