using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using System.Security.Claims;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PomodoroController : ControllerBase
{
    private readonly IPomodoroService _pomodoroService;

    public PomodoroController(IPomodoroService pomodoroService)
    {
        _pomodoroService = pomodoroService;
    }

    #region Pomodoro Session Endpoints

    /// <summary>
    /// Yeni pomodoro oturumu başlatır
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartPomodoro([FromBody] StartPomodoroRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.StartPomodoroAsync(userId.Value, request);

        if (result == null)
            return BadRequest(new { message = "Pomodoro başlatılamadı. Aktif bir oturum olabilir." });

        return Ok(result);
    }

    /// <summary>
    /// Pomodoro oturumunu tamamlar ve XP kazandırır
    /// </summary>
    [HttpPost("{sessionId}/complete")]
    public async Task<IActionResult> CompletePomodoro(int sessionId, [FromBody] CompletePomodoroRequest? request = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.CompletePomodoroAsync(userId.Value, sessionId, request);

        if (result == null)
            return NotFound(new { message = "Oturum bulunamadı veya zaten tamamlanmış" });

        return Ok(result);
    }

    /// <summary>
    /// Pomodoro oturumunu iptal eder
    /// </summary>
    [HttpPost("{sessionId}/cancel")]
    public async Task<IActionResult> CancelPomodoro(int sessionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.CancelPomodoroAsync(userId.Value, sessionId);

        if (!result)
            return NotFound(new { message = "Oturum bulunamadı veya zaten tamamlanmış" });

        return Ok(new { message = "Pomodoro iptal edildi" });
    }

    /// <summary>
    /// Aktif pomodoro oturumunu getirir
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActivePomodoro()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.GetActivePomodoroAsync(userId.Value);

        if (result == null)
            return Ok(new { message = "Aktif oturum yok", session = (object?)null });

        return Ok(result);
    }

    /// <summary>
    /// Pomodoro geçmişini getirir
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetPomodoroHistory([FromQuery] int? subjectId = null, [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.GetPomodoroHistoryAsync(userId.Value, subjectId, limit);

        return Ok(result);
    }

    /// <summary>
    /// Pomodoro istatistiklerini getirir
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetPomodoroStats()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.GetPomodoroStatsAsync(userId.Value);

        return Ok(result);
    }

    #endregion

    #region Study Schedule Endpoints

    /// <summary>
    /// Ders programı ekler
    /// </summary>
    [HttpPost("schedule")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.CreateScheduleAsync(userId.Value, request);

        if (result == null)
            return BadRequest(new { message = "Program eklenemedi. Ders veya saat bilgisi hatalı olabilir." });

        return Ok(result);
    }

    /// <summary>
    /// Ders programını günceller
    /// </summary>
    [HttpPut("schedule/{scheduleId}")]
    public async Task<IActionResult> UpdateSchedule(int scheduleId, [FromBody] UpdateScheduleRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.UpdateScheduleAsync(userId.Value, scheduleId, request);

        if (result == null)
            return NotFound(new { message = "Program bulunamadı" });

        return Ok(result);
    }

    /// <summary>
    /// Ders programını siler
    /// </summary>
    [HttpDelete("schedule/{scheduleId}")]
    public async Task<IActionResult> DeleteSchedule(int scheduleId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.DeleteScheduleAsync(userId.Value, scheduleId);

        if (!result)
            return NotFound(new { message = "Program bulunamadı" });

        return NoContent();
    }

    /// <summary>
    /// Haftalık ders programını getirir
    /// </summary>
    [HttpGet("schedule/weekly")]
    public async Task<IActionResult> GetWeeklySchedule()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.GetWeeklyScheduleAsync(userId.Value);

        return Ok(result);
    }

    /// <summary>
    /// Bugünün ders programını getirir
    /// </summary>
    [HttpGet("schedule/today")]
    public async Task<IActionResult> GetTodaySchedule()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _pomodoroService.GetTodayScheduleAsync(userId.Value);

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

