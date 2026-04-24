using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Teacher")] // Sadece öğretmenler erişebilir
public class TeacherPanelController : ControllerBase
{
    private readonly ITeacherPanelService _teacherPanelService;
    private readonly IAppointmentService _appointmentService;

    public TeacherPanelController(ITeacherPanelService teacherPanelService, IAppointmentService appointmentService)
    {
        _teacherPanelService = teacherPanelService;
        _appointmentService = appointmentService;
    }

    private int GetTeacherId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out int id))
        {
            return id;
        }
        throw new UnauthorizedAccessException("Geçersiz öğretmen kimliği.");
    }

    #region Classrooms

    [HttpGet("classrooms")]
    public async Task<IActionResult> GetClassrooms()
    {
        var teacherId = GetTeacherId();
        var classrooms = await _teacherPanelService.GetTeacherClassroomsAsync(teacherId);
        return Ok(classrooms);
    }

    [HttpPost("classrooms")]
    public async Task<IActionResult> CreateClassroom([FromBody] CreateClassroomRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var teacherId = GetTeacherId();
        var result = await _teacherPanelService.CreateClassroomAsync(teacherId, request);
        return Created("", result);
    }

    [HttpPost("classrooms/{classroomId}/students/{studentId}")]
    public async Task<IActionResult> AddStudentToClassroom(int classroomId, int studentId)
    {
        var teacherId = GetTeacherId();
        var success = await _teacherPanelService.AddStudentToClassroomAsync(teacherId, classroomId, studentId);
        
        if (!success) return BadRequest(new { Message = "Öğrenci eklenemedi veya bu sınıfa yetkiniz yok." });
        return Ok(new { Message = "Öğrenci başarıyla eklendi." });
    }

    [HttpDelete("classrooms/{classroomId}/students/{studentId}")]
    public async Task<IActionResult> RemoveStudentFromClassroom(int classroomId, int studentId)
    {
        var teacherId = GetTeacherId();
        var success = await _teacherPanelService.RemoveStudentFromClassroomAsync(teacherId, classroomId, studentId);
        
        if (!success) return BadRequest(new { Message = "Öğrenci silinemedi veya bu sınıfa yetkiniz yok." });
        return Ok(new { Message = "Öğrenci başarıyla sınıftan çıkarıldı." });
    }

    #endregion

    #region Announcements

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements()
    {
        var teacherId = GetTeacherId();
        var announcements = await _teacherPanelService.GetTeacherAnnouncementsAsync(teacherId);
        return Ok(announcements);
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        if (request.ClassroomId == null && request.UserId == null)
            return BadRequest(new { Message = "Duyuru için bir sınıf veya öğrenci seçmelisiniz." });

        var teacherId = GetTeacherId();
        var result = await _teacherPanelService.CreateAnnouncementAsync(teacherId, request);
        return Created("", result);
    }

    #endregion

    #region Homeworks

    [HttpGet("homeworks")]
    public async Task<IActionResult> GetHomeworks()
    {
        var teacherId = GetTeacherId();
        var homeworks = await _teacherPanelService.GetTeacherHomeworksAsync(teacherId);
        return Ok(homeworks);
    }

    [HttpPost("homeworks")]
    public async Task<IActionResult> CreateHomework([FromBody] CreateHomeworkRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        if (request.ClassroomId == null && request.UserId == null)
            return BadRequest(new { Message = "Ödev için bir sınıf veya öğrenci seçmelisiniz." });

        var teacherId = GetTeacherId();
        var result = await _teacherPanelService.CreateHomeworkAsync(teacherId, request);
        return Created("", result);
    }

    [HttpGet("homeworks/{id}/status")]
    public async Task<IActionResult> GetHomeworkStatus(int id)
    {
        var teacherId = GetTeacherId();
        var statusList = await _teacherPanelService.GetHomeworkStatusListAsync(teacherId, id);
        return Ok(statusList);
    }

    #endregion

    #region Student Monitoring

    [HttpGet("classrooms/{classroomId}/performances")]
    public async Task<IActionResult> GetStudentPerformances(int classroomId)
    {
        var teacherId = GetTeacherId();
        var performances = await _teacherPanelService.GetClassroomStudentPerformancesAsync(teacherId, classroomId);
        return Ok(performances);
    }

    [HttpPost("classrooms/{classroomId}/students/{studentId}/trial-exams")]
    public async Task<IActionResult> AddStudentTrialExam(int classroomId, int studentId, [FromBody] CreateTrialExamRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var teacherId = GetTeacherId();
        try
        {
            var result = await _teacherPanelService.AddStudentTrialExamAsync(teacherId, studentId, request);
            if (result == null) 
                return BadRequest(new { Message = "Öğrenci bulunamadı veya bu öğrencinin denemesine yetkiniz yok." });
            
            return Created("", result);
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest(new { Message = "Bu öğrenciye deneme ekleme yetkiniz yok." });
        }
    }

    [HttpGet("classrooms/{classroomId}/students/{studentId}/trial-exams")]
    public async Task<IActionResult> GetStudentTrialExams(int classroomId, int studentId, [FromQuery] string? examType = null)
    {
        var teacherId = GetTeacherId();
        try
        {
            var result = await _teacherPanelService.GetStudentTrialExamsAsync(teacherId, studentId, examType);
            if (result == null) 
                return BadRequest(new { Message = "Öğrenci bulunamadı veya bu öğrencinin denemelerine yetkiniz yok." });
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest(new { Message = "Bu öğrencinin denemelerini görme yetkiniz yok." });
        }
    }

    [HttpGet("classrooms/{classroomId}/students/{studentId}/trial-exams/{trialId}")]
    public async Task<IActionResult> GetStudentTrialExamDetail(int classroomId, int studentId, int trialId)
    {
        var teacherId = GetTeacherId();
        try
        {
            var result = await _teacherPanelService.GetStudentTrialExamDetailAsync(teacherId, studentId, trialId);
            if (result == null) 
                return NotFound(new { Message = "Deneme veya öğrenci bulunamadı." });
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest(new { Message = "Bu öğrencinin deneme detayını görme yetkiniz yok." });
        }
    }

    #endregion

    #region Appointments

    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments()
    {
        var teacherId = GetTeacherId();
        var appointments = await _appointmentService.GetTeacherAppointmentsAsync(teacherId);
        return Ok(appointments);
    }

    [HttpPut("appointments/{id}/schedule")]
    public async Task<IActionResult> ScheduleAppointment(int id, [FromBody] ScheduleAppointmentDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var teacherId = GetTeacherId();
        try
        {
            var result = await _appointmentService.ScheduleAppointmentAsync(teacherId, id, request);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    #endregion
}
