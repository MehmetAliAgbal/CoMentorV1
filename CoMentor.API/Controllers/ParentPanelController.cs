using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using CoMentor.Application.Interfaces;
using CoMentor.Application.DTOs;

namespace CoMentor.API.Controllers
{
    [Authorize(Roles = "Parent")]
    [ApiController]
    [Route("api/[controller]")]
    public class ParentPanelController : ControllerBase
    {
        private readonly IParentPanelService _parentPanelService;
        private readonly IAppointmentService _appointmentService;

        public ParentPanelController(IParentPanelService parentPanelService, IAppointmentService appointmentService)
        {
            _parentPanelService = parentPanelService;
            _appointmentService = appointmentService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int parentId))
            {
                return Unauthorized(new { Message = "Sistem üzerinden geçerli bir Veli kimliği bulunamadı." });
            }

            try
            {
                var dashboard = await _parentPanelService.GetDashboardAsync(parentId);
                return Ok(dashboard);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int parentId))
            {
                return Unauthorized(new { Message = "Sistem üzerinden geçerli bir Veli kimliği bulunamadı." });
            }

            try
            {
                var messages = await _parentPanelService.GetMessagesAsync(parentId);
                return Ok(messages);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("appointments/request")]
        public async Task<IActionResult> RequestAppointment([FromBody] RequestAppointmentDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int parentId))
                return Unauthorized(new { Message = "Kimlik bulunamadı." });

            try
            {
                var appointment = await _appointmentService.RequestAppointmentAsync(parentId, "Parent", request);
                return Ok(appointment);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
