using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using System.Security.Claims;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrialExamController : ControllerBase
{
    private readonly ITrialExamService _trialExamService;

    public TrialExamController(ITrialExamService trialExamService)
    {
        _trialExamService = trialExamService;
    }

    /// <summary>
    /// Yeni deneme kaydı oluşturur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTrialExam([FromBody] CreateTrialExamRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _trialExamService.CreateTrialExamAsync(userId.Value, request);

        if (result == null)
            return BadRequest(new { message = "Deneme oluşturulamadı" });

        return CreatedAtAction(nameof(GetTrialExamById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Deneme bilgilerini günceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrialExam(int id, [FromBody] UpdateTrialExamRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _trialExamService.UpdateTrialExamAsync(userId.Value, id, request);

        if (result == null)
            return NotFound(new { message = "Deneme bulunamadı" });

        return Ok(result);
    }

    /// <summary>
    /// Deneme kaydını siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrialExam(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _trialExamService.DeleteTrialExamAsync(userId.Value, id);

        if (!result)
            return NotFound(new { message = "Deneme bulunamadı" });

        return NoContent();
    }

    /// <summary>
    /// Belirli bir denemenin detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTrialExamById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _trialExamService.GetTrialExamByIdAsync(userId.Value, id);

        if (result == null)
            return NotFound(new { message = "Deneme bulunamadı" });

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının tüm denemelerini listeler (istatistiklerle birlikte)
    /// </summary>
    /// <param name="examType">Opsiyonel filtre: TYT veya AYT</param>
    [HttpGet]
    public async Task<IActionResult> GetTrialExams([FromQuery] string? examType = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _trialExamService.GetTrialExamsAsync(userId.Value, examType);

        return Ok(result);
    }

    /// <summary>
    /// Kullanıcının deneme istatistiklerini getirir
    /// </summary>
    /// <param name="examType">Opsiyonel filtre: TYT veya AYT</param>
    [HttpGet("stats")]
    public async Task<IActionResult> GetTrialExamStats([FromQuery] string? examType = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı" });

        var result = await _trialExamService.GetTrialExamStatsAsync(userId.Value, examType);

        return Ok(result);
    }

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

