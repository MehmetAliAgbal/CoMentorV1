using System.Security.Claims;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AchievementsController : ControllerBase
{
    private readonly IAchievementService _achievementService;

    public AchievementsController(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        // Önce kontrol et ve hak edilenleri ver
        await _achievementService.CheckAndGrantAchievementsAsync(userId);
        
        // Sonra listeyi dön
        var achievements = await _achievementService.GetAchievementsAsync(userId);
        return Ok(achievements);
    }

    [HttpGet("my-achievements")]
    public async Task<IActionResult> GetMyAchievements()
    {
        var userId = GetUserId();
        var myAchievements = await _achievementService.GetUserAchievementsAsync(userId);
        return Ok(myAchievements);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // Sadece admin ekleyebilir (Authentication'da Role varsa)
    public async Task<IActionResult> Create([FromBody] CreateAchievementRequest request)
    {
        var result = await _achievementService.CreateAchievementAsync(request);
        return Ok(result);
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
