using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CoMentor.Application.Interfaces;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Öğrenciler erişebilir (veya teacher da)
public class StudentPanelController : ControllerBase
{
    private readonly ITeacherPanelService _teacherPanelService;

    public StudentPanelController(ITeacherPanelService teacherPanelService)
    {
        _teacherPanelService = teacherPanelService;
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
}
