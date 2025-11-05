using Microsoft.AspNetCore.Mvc;
using CoMentor.Application.DTOs;
using CoMentor.Infrastructure.Services;
using CoMentor.Application.Interfaces;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var res = await _auth.RegisterAsync(req);
        if (res == null) return BadRequest(new { message = "Email already in use" });
        return Ok(res);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var res = await _auth.LoginAsync(req);
        if (res == null) return Unauthorized(new { message = "Invalid credentials" });
        return Ok(res);
    }
}