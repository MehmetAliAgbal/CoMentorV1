using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MlPredictionController : ControllerBase
{
    private readonly IMlPredictionService _mlPredictionService;

    public MlPredictionController(IMlPredictionService mlPredictionService)
    {
        _mlPredictionService = mlPredictionService;
    }

    [HttpPost("sayisal-tahmin")]
    public async Task<IActionResult> SayisalTahminYap([FromBody] SayisalTahminRequest request)
    {
        var sonucJson = await _mlPredictionService.GetSayisalTahminAsync(request);
        if (string.IsNullOrEmpty(sonucJson)) return BadRequest("Makine Öğrenmesi API'sine ulaşılamadı.");
        return Content(sonucJson, "application/json");
    }

    [HttpPost("tyt-tahmin")]
    public async Task<IActionResult> TytTahminYap([FromBody] TytTahminRequest request)
    {
        var sonucJson = await _mlPredictionService.GetTytTahminAsync(request);
        if (string.IsNullOrEmpty(sonucJson)) return BadRequest("Makine Öğrenmesi API'sine ulaşılamadı.");
        return Content(sonucJson, "application/json");
    }
}
