using Microsoft.AspNetCore.Mvc;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParentAuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public ParentAuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] ParentRegisterRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var res = await _auth.ParentRegisterAsync(req);
        if (res == null) return BadRequest(new { message = "E-posta zaten kullanılıyor veya öğrenci bulunamadı." });
        return Ok(res);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var res = await _auth.ParentLoginAsync(req);
        if (res == null) return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });
        return Ok(res);
    }
}
