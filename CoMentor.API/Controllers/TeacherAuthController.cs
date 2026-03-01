using Microsoft.AspNetCore.Mvc;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherAuthController : ControllerBase
{
    private readonly ITeacherAuthService _teacherAuthService;

    public TeacherAuthController(ITeacherAuthService teacherAuthService)
    {
        _teacherAuthService = teacherAuthService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] TeacherRegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _teacherAuthService.RegisterAsync(request);
        if (result == null)
            return BadRequest(new { Message = "Email is already registered" });

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] TeacherLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _teacherAuthService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { Message = "Invalid email or password" });

        return Ok(result);
    }
}
