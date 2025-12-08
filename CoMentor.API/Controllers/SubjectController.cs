using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CoMentor.Application.DTOs;
using CoMentor.Domain.Entities;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubjectController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tüm dersleri listeler
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllSubjects([FromQuery] string? examType = null)
    {
        var query = _db.Subjects.Where(s => s.IsActive);

        if (!string.IsNullOrEmpty(examType))
        {
            query = query.Where(s => s.ExamType == examType || s.ExamType == "BOTH");
        }

        var subjects = await query
            .OrderBy(s => s.Id)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                ShortName = s.ShortName,
                ColorHex = s.ColorHex,
                ExamType = s.ExamType,
                MaxQuestions = s.MaxQuestions,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(subjects);
    }

    /// <summary>
    /// Yeni ders ekler
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var subject = new Subject
        {
            Name = request.Name,
            ShortName = request.ShortName,
            ColorHex = request.ColorHex,
            ExamType = request.ExamType,
            MaxQuestions = request.MaxQuestions,
            IsActive = request.IsActive
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        return Ok(new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name,
            ShortName = subject.ShortName,
            ColorHex = subject.ColorHex,
            ExamType = subject.ExamType,
            MaxQuestions = subject.MaxQuestions,
            IsActive = subject.IsActive
        });
    }

    /// <summary>
    /// TYT ve AYT için varsayılan dersleri ekler (Seed Data)
    /// </summary>
    [HttpPost("seed")]
    [Authorize]
    public async Task<IActionResult> SeedSubjects()
    {
        // Eğer zaten ders varsa ekleme yapma
        if (await _db.Subjects.AnyAsync())
        {
            return BadRequest(new { message = "Dersler zaten mevcut. Tekrar seed yapmak için önce mevcut dersleri silin." });
        }

        var subjects = new List<Subject>
        {
            // TYT Dersleri
            new Subject { Name = "Türkçe", ShortName = "TUR", ColorHex = "#E74C3C", ExamType = "TYT", MaxQuestions = 40, IsActive = true },
            new Subject { Name = "Matematik", ShortName = "MAT", ColorHex = "#3498DB", ExamType = "TYT", MaxQuestions = 40, IsActive = true },
            new Subject { Name = "Fen Bilimleri", ShortName = "FEN", ColorHex = "#2ECC71", ExamType = "TYT", MaxQuestions = 20, IsActive = true },
            new Subject { Name = "Sosyal Bilimler", ShortName = "SOS", ColorHex = "#9B59B6", ExamType = "TYT", MaxQuestions = 20, IsActive = true },

            // AYT Sayısal Dersleri
            new Subject { Name = "AYT Matematik", ShortName = "AYT-MAT", ColorHex = "#2980B9", ExamType = "AYT", MaxQuestions = 40, IsActive = true },
            new Subject { Name = "Fizik", ShortName = "FIZ", ColorHex = "#E67E22", ExamType = "AYT", MaxQuestions = 14, IsActive = true },
            new Subject { Name = "Kimya", ShortName = "KIM", ColorHex = "#1ABC9C", ExamType = "AYT", MaxQuestions = 13, IsActive = true },
            new Subject { Name = "Biyoloji", ShortName = "BIY", ColorHex = "#27AE60", ExamType = "AYT", MaxQuestions = 13, IsActive = true },

            // AYT Eşit Ağırlık Dersleri
            new Subject { Name = "Edebiyat", ShortName = "EDB", ColorHex = "#C0392B", ExamType = "AYT", MaxQuestions = 24, IsActive = true },
            new Subject { Name = "Tarih-1", ShortName = "TAR1", ColorHex = "#8E44AD", ExamType = "AYT", MaxQuestions = 10, IsActive = true },
            new Subject { Name = "Coğrafya-1", ShortName = "COG1", ColorHex = "#16A085", ExamType = "AYT", MaxQuestions = 6, IsActive = true },

            // AYT Sözel Dersleri
            new Subject { Name = "Tarih-2", ShortName = "TAR2", ColorHex = "#9B59B6", ExamType = "AYT", MaxQuestions = 11, IsActive = true },
            new Subject { Name = "Coğrafya-2", ShortName = "COG2", ColorHex = "#1ABC9C", ExamType = "AYT", MaxQuestions = 11, IsActive = true },
            new Subject { Name = "Felsefe Grubu", ShortName = "FEL", ColorHex = "#34495E", ExamType = "AYT", MaxQuestions = 12, IsActive = true },
            new Subject { Name = "Din Kültürü", ShortName = "DIN", ColorHex = "#F39C12", ExamType = "AYT", MaxQuestions = 6, IsActive = true },

            // Yabancı Dil
            new Subject { Name = "Yabancı Dil (İngilizce)", ShortName = "YDL", ColorHex = "#3498DB", ExamType = "AYT", MaxQuestions = 80, IsActive = true }
        };

        _db.Subjects.AddRange(subjects);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Dersler başarıyla eklendi",
            count = subjects.Count,
            subjects = subjects.Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                ShortName = s.ShortName,
                ColorHex = s.ColorHex,
                ExamType = s.ExamType,
                MaxQuestions = s.MaxQuestions,
                IsActive = s.IsActive
            })
        });
    }

    /// <summary>
    /// Belirli bir dersi siler
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject == null)
            return NotFound(new { message = "Ders bulunamadı" });

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

