using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoMentor.Application.Interfaces;
using System.Security.Claims;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    #region Leaderboard Endpoints

    /// <summary>
    /// Genel XP sıralamasını getirir (En yüksek XP'den düşüğe)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetGeneralLeaderboard([FromQuery] int limit = 100)
    {
        var userId = GetCurrentUserId();
        var result = await _leaderboardService.GetGeneralLeaderboardAsync(userId, limit);

        return Ok(result);
    }

    /// <summary>
    /// Okul ligi sıralamasını getirir (aynı okuldaki kullanıcılar)
    /// </summary>
    [HttpGet("school")]
    [Authorize]
    public async Task<IActionResult> GetSchoolLeaderboard([FromQuery] int limit = 100)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _leaderboardService.GetSchoolLeaderboardAsync(userId.Value, limit);

        return Ok(result);
    }

    /// <summary>
    /// Sınıf ligi sıralamasını getirir (aynı sınıftaki kullanıcılar)
    /// </summary>
    [HttpGet("grade")]
    [Authorize]
    public async Task<IActionResult> GetGradeLeaderboard([FromQuery] int limit = 100)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _leaderboardService.GetGradeLeaderboardAsync(userId.Value, limit);

        return Ok(result);
    }

    /// <summary>
    /// Tüm ligleri tek seferde getirir (Genel, Okul, Sınıf)
    /// </summary>
    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetAllLeagues([FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _leaderboardService.GetAllLeaguesAsync(userId.Value, limit);

        return Ok(result);
    }

    #endregion

    #region XP Endpoints

    /// <summary>
    /// Kullanıcının XP geçmişini getirir
    /// </summary>
    [HttpGet("xp/history")]
    [Authorize]
    public async Task<IActionResult> GetXpHistory([FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _leaderboardService.GetXpHistoryAsync(userId.Value, limit);

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının XP özetini getirir (toplam, bugün, hafta, sıralamalar)
    /// </summary>
    [HttpGet("xp/summary")]
    [Authorize]
    public async Task<IActionResult> GetXpSummary()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _leaderboardService.GetUserXpSummaryAsync(userId.Value);

        return Ok(result);
    }

    #endregion

    #region Private Helper Methods

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    #endregion
}

