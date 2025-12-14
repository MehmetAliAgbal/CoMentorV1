using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoMentor.Application.Interfaces;
using System.Security.Claims;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeagueController : ControllerBase
{
    private readonly ILeagueService _leagueService;

    public LeagueController(ILeagueService leagueService)
    {
        _leagueService = leagueService;
    }

    #region Lig Bilgileri

    /// <summary>
    /// Tüm ligleri getirir (Bronz, Gümüş, Altın, Platin, Elmas)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllLeagues()
    {
        var leagues = await _leagueService.GetAllLeaguesAsync();
        return Ok(leagues);
    }

    /// <summary>
    /// Belirli bir ligi getirir
    /// </summary>
    [HttpGet("{leagueId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeagueById(int leagueId)
    {
        var league = await _leagueService.GetLeagueByIdAsync(leagueId);
        if (league == null)
            return NotFound(new { message = "Lig bulunamadı" });

        return Ok(league);
    }

    /// <summary>
    /// Tüm liglerin genel görünümünü getirir (kullanıcı sayılarıyla birlikte)
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetAllLeaguesOverview()
    {
        var userId = GetCurrentUserId();
        var overview = await _leagueService.GetAllLeaguesOverviewAsync(userId);
        return Ok(overview);
    }

    #endregion

    #region Kullanıcı Lig Durumu

    /// <summary>
    /// Mevcut kullanıcının lig durumunu getirir
    /// </summary>
    [HttpGet("my-league")]
    [Authorize]
    public async Task<IActionResult> GetMyLeague()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var userLeague = await _leagueService.GetUserLeagueAsync(userId.Value);
        if (userLeague == null)
            return NotFound(new { message = "Lig bilgisi bulunamadı" });

        return Ok(userLeague);
    }

    /// <summary>
    /// Belirli bir kullanıcının lig durumunu getirir
    /// </summary>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserLeague(int userId)
    {
        var userLeague = await _leagueService.GetUserLeagueAsync(userId);
        if (userLeague == null)
            return NotFound(new { message = "Kullanıcı veya lig bilgisi bulunamadı" });

        return Ok(userLeague);
    }

    /// <summary>
    /// Kullanıcının lig geçmişini getirir
    /// </summary>
    [HttpGet("my-history")]
    [Authorize]
    public async Task<IActionResult> GetMyLeagueHistory()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var history = await _leagueService.GetUserLeagueHistoryAsync(userId.Value);
        return Ok(history);
    }

    #endregion

    #region Lig Sıralaması

    /// <summary>
    /// Belirli bir ligteki kullanıcı sıralamasını getirir
    /// </summary>
    [HttpGet("{leagueId}/leaderboard")]
    public async Task<IActionResult> GetLeagueLeaderboard(int leagueId, [FromQuery] int limit = 100)
    {
        var userId = GetCurrentUserId();
        var leaderboard = await _leagueService.GetLeagueLeaderboardAsync(leagueId, userId, limit);
        
        if (leaderboard == null)
            return NotFound(new { message = "Lig bulunamadı" });

        return Ok(leaderboard);
    }

    /// <summary>
    /// Mevcut kullanıcının ligindeki sıralamayı getirir
    /// </summary>
    [HttpGet("my-league/leaderboard")]
    [Authorize]
    public async Task<IActionResult> GetMyLeagueLeaderboard([FromQuery] int limit = 100)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var userLeague = await _leagueService.GetUserLeagueAsync(userId.Value);
        if (userLeague == null)
            return NotFound(new { message = "Lig bilgisi bulunamadı" });

        var leaderboard = await _leagueService.GetLeagueLeaderboardAsync(
            userLeague.CurrentLeague.Id, userId, limit);

        return Ok(leaderboard);
    }

    #endregion

    #region Lig Güncelleme

    /// <summary>
    /// Kullanıcının ligini kontrol eder ve gerekirse günceller
    /// (XP değiştiğinde otomatik çağrılabilir)
    /// </summary>
    [HttpPost("check-update")]
    [Authorize]
    public async Task<IActionResult> CheckAndUpdateLeague()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _leagueService.CheckAndUpdateUserLeagueAsync(userId.Value);
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

