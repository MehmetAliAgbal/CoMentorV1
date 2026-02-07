using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CoMentor.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StudyScheduleController : ControllerBase
    {
        private readonly IAIStudyCoachService _aiStudyCoachService;

        public StudyScheduleController(IAIStudyCoachService aiStudyCoachService)
        {
            _aiStudyCoachService = aiStudyCoachService;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<List<StudyScheduleDto>>> GenerateSchedule([FromBody] GenerateScheduleRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest("Invalid User ID");

            try
            {
                var schedule = await _aiStudyCoachService.GenerateScheduleAsync(userId, request);
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                // In production, log the error
                return StatusCode(500, $"An error occurred while generating the schedule: {ex.Message}");
            }
        }


    }
}
