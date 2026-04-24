using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CoMentor.Application.Interfaces;
using CoMentor.Application.DTOs;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Öğrenciler erişebilir (veya teacher da)
public class StudentPanelController : ControllerBase
{
    private readonly ITeacherPanelService _teacherPanelService;
    private readonly IAppointmentService _appointmentService;

    public StudentPanelController(ITeacherPanelService teacherPanelService, IAppointmentService appointmentService)
    {
        _teacherPanelService = teacherPanelService;
        _appointmentService = appointmentService;
    }

    private int GetStudentId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out int id))
        {
            return id;
        }
        throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği.");
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> GetMyAnnouncements()
    {
        var studentId = GetStudentId();
        var announcements = await _teacherPanelService.GetStudentAnnouncementsAsync(studentId);
        return Ok(announcements);
    }

    [HttpGet("homeworks")]
    public async Task<IActionResult> GetMyHomeworks()
    {
        var studentId = GetStudentId();
        var homeworks = await _teacherPanelService.GetStudentHomeworksAsync(studentId);
        return Ok(homeworks);
    }

    [HttpPut("homeworks/{id}/complete")]
    public async Task<IActionResult> CompleteHomework(int id)
    {
        var studentId = GetStudentId();
        var success = await _teacherPanelService.MarkHomeworkAsCompletedAsync(studentId, id);
        
        if (!success)
            return BadRequest(new { Message = "Ödev bulunamadı veya işlem başarısız." });
            
        return Ok(new { Message = "Ödev başarıyla tamamlandı olarak işaretlendi." });
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var studentId = GetStudentId();
        var result = await _appointmentService.GetStudentAppointmentsAsync(studentId);
        return Ok(result);
    }
    
    [HttpPost("appointments/request")]
    public async Task<IActionResult> RequestAppointment([FromBody] RequestAppointmentDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var studentId = GetStudentId();
        try
        {
            var result = await _appointmentService.RequestAppointmentAsync(studentId, "Student", request);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
