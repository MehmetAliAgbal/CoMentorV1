using CoMentor.Application.DTOs;
using CoMentor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace CoMentor.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VideoRecommendationController : ControllerBase
    {
        private readonly AppDbContext _db;

        public VideoRecommendationController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("random")]
        public async Task<ActionResult<VideoRecommendationDto>> GetRandomDailyRecommendation()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest("Invalid User ID");

            return await GetRecommendationForUser(userId);
        }

        private async Task<ActionResult<VideoRecommendationDto>> GetRecommendationForUser(int userId)
        {
            // Rastgele (henüz izlenmemiş) bir video tavsiyesini getir
            // Postgres / SQL Server'da Guid.NewGuid() kullanarak order by rastgele çekilebilir
            var randomRecommendation = await _db.VideoRecommendations
                .Include(v => v.Subject)
                .Where(v => v.UserId == userId && !v.IsWatched)
                .OrderBy(v => System.Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (randomRecommendation == null)
            {
                // Eğer izlenmemiş kalmadıysa veya hiç yoksa null/NotFound dönebiliriz.
                return NotFound("No unseen video recommendations available.");
            }

            var dto = new VideoRecommendationDto
            {
                Id = randomRecommendation.Id,
                SubjectId = randomRecommendation.SubjectId,
                SubjectName = randomRecommendation.Subject?.Name ?? "",
                Title = randomRecommendation.Title,
                Url = randomRecommendation.Url,
                RecommendedAt = randomRecommendation.RecommendedAt,
                IsWatched = randomRecommendation.IsWatched
            };

            return Ok(dto);
        }
    }
}
