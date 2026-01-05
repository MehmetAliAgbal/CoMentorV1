using System.Security.Claims;
using CoMentor.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudyStreaksController : ControllerBase
{
    private readonly IStudyStreakService _streakService;

    public StudyStreaksController(IStudyStreakService streakService)
    {
        _streakService = streakService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetUserId();
        
        // İsteğe bağlı: Kullanıcı her status kontrol ettiğinde de streak güncellenebilir
        // Veya sadece login'de yapılır.
        // Güvenlik açısından, sadece "Get" isteği streak arttırmamalı.
        // Ancak kullanıcı dashboard'u açtığında streak'in güncel durumu görmeli.
        // Eğer "bugün girdi" sayılacaksa, burada UpdateStreakAsync çağrılabilir.
        // Karar: Bu endpoint sadece okuma yapmalı. Streak artışı Login'de veya özel bir "Check-in" işleminde olmalı.
        // Ama kullanıcı "Uygulamayı açtı" ise bu bir aktivitedir.
        // Basitlik adına burada çağırmıyorum, sadece durumu dönüyorum.
        
        var status = await _streakService.GetUserStreakStatusAsync(userId);
        return Ok(status);
    }
    
    // Geliştirme/Test amaçlı manuel tetikleme
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn()
    {
        var userId = GetUserId();
        await _streakService.UpdateStreakAsync(userId);
        var status = await _streakService.GetUserStreakStatusAsync(userId);
        return Ok(status);
    }

    private int GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in token");
    }
}
