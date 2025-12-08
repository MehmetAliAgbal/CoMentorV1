using Microsoft.EntityFrameworkCore;
using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services;

public class TrialExamService : ITrialExamService
{
    private readonly AppDbContext _db;

    public TrialExamService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Yeni deneme kaydı oluşturur
    /// </summary>
    public async Task<TrialExamDto?> CreateTrialExamAsync(int userId, CreateTrialExamRequest request)
    {
        // Kullanıcının var olup olmadığını kontrol et
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return null;

        var trialExam = new TrialExam
        {
            UserId = userId,
            Name = request.Name,
            ExamType = request.ExamType,
            ExamDate = DateTime.SpecifyKind(request.ExamDate, DateTimeKind.Utc),
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.TrialExams.Add(trialExam);
        await _db.SaveChangesAsync();

        // Ders bazlı skorları ekle
        foreach (var scoreReq in request.SubjectScores)
        {
            var subjectScore = new TrialSubjectScore
            {
                TrialExamId = trialExam.Id,
                SubjectId = scoreReq.SubjectId,
                CorrectAnswers = scoreReq.CorrectAnswers,
                WrongAnswers = scoreReq.WrongAnswers,
                EmptyAnswers = scoreReq.EmptyAnswers
            };
            _db.TrialSubjectScores.Add(subjectScore);
        }

        await _db.SaveChangesAsync();

        // Toplam neti hesapla ve kaydet
        var totalNet = await CalculateTotalNetAsync(trialExam.Id);
        trialExam.TotalScore = (int)Math.Round(totalNet);
        await _db.SaveChangesAsync();

        return await GetTrialExamByIdAsync(userId, trialExam.Id);
    }

    /// <summary>
    /// Deneme bilgilerini günceller
    /// </summary>
    public async Task<TrialExamDto?> UpdateTrialExamAsync(int userId, int trialId, UpdateTrialExamRequest request)
    {
        var trialExam = await _db.TrialExams
            .Include(t => t.SubjectScores)
            .FirstOrDefaultAsync(t => t.Id == trialId && t.UserId == userId);

        if (trialExam == null)
            return null;

        // Güncellenebilir alanları güncelle
        if (!string.IsNullOrEmpty(request.Name))
            trialExam.Name = request.Name;

        if (request.ExamDate.HasValue)
            trialExam.ExamDate = DateTime.SpecifyKind(request.ExamDate.Value, DateTimeKind.Utc);

        if (request.DurationMinutes.HasValue)
            trialExam.DurationMinutes = request.DurationMinutes.Value;

        if (request.Notes != null)
            trialExam.Notes = request.Notes;

        // Ders skorlarını güncelle
        if (request.SubjectScores != null && request.SubjectScores.Any())
        {
            // Mevcut skorları sil
            _db.TrialSubjectScores.RemoveRange(trialExam.SubjectScores);

            // Yeni skorları ekle
            foreach (var scoreReq in request.SubjectScores)
            {
                var subjectScore = new TrialSubjectScore
                {
                    TrialExamId = trialExam.Id,
                    SubjectId = scoreReq.SubjectId,
                    CorrectAnswers = scoreReq.CorrectAnswers,
                    WrongAnswers = scoreReq.WrongAnswers,
                    EmptyAnswers = scoreReq.EmptyAnswers
                };
                _db.TrialSubjectScores.Add(subjectScore);
            }

            await _db.SaveChangesAsync();

            // Toplam neti güncelle
            var totalNet = await CalculateTotalNetAsync(trialExam.Id);
            trialExam.TotalScore = (int)Math.Round(totalNet);
        }

        await _db.SaveChangesAsync();

        return await GetTrialExamByIdAsync(userId, trialId);
    }

    /// <summary>
    /// Deneme kaydını siler
    /// </summary>
    public async Task<bool> DeleteTrialExamAsync(int userId, int trialId)
    {
        var trialExam = await _db.TrialExams
            .Include(t => t.SubjectScores)
            .FirstOrDefaultAsync(t => t.Id == trialId && t.UserId == userId);

        if (trialExam == null)
            return false;

        // Önce ders skorlarını sil
        _db.TrialSubjectScores.RemoveRange(trialExam.SubjectScores);
        _db.TrialExams.Remove(trialExam);
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Belirli bir denemenin detayını getirir
    /// </summary>
    public async Task<TrialExamDto?> GetTrialExamByIdAsync(int userId, int trialId)
    {
        var trialExam = await _db.TrialExams
            .Include(t => t.SubjectScores)
                .ThenInclude(s => s.Subject)
            .FirstOrDefaultAsync(t => t.Id == trialId && t.UserId == userId);

        if (trialExam == null)
            return null;

        return MapToDto(trialExam);
    }

    /// <summary>
    /// Kullanıcının tüm denemelerini istatistiklerle birlikte getirir
    /// </summary>
    public async Task<TrialExamListResponse> GetTrialExamsAsync(int userId, string? examType = null)
    {
        var query = _db.TrialExams
            .Include(t => t.SubjectScores)
                .ThenInclude(s => s.Subject)
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(examType))
            query = query.Where(t => t.ExamType == examType);

        var trials = await query
            .OrderByDescending(t => t.ExamDate)
            .ToListAsync();

        var trialDtos = new List<TrialExamListItemDto>();
        double? previousNet = null;

        // Tarihe göre sıralanmış (eskiden yeniye) denemeleri işle
        var orderedTrials = trials.OrderBy(t => t.ExamDate).ToList();

        for (int i = 0; i < orderedTrials.Count; i++)
        {
            var trial = orderedTrials[i];
            var totalNet = trial.SubjectScores.Sum(s => s.CorrectAnswers - (s.WrongAnswers / 4.0));
            double? netChange = null;

            if (previousNet.HasValue)
            {
                netChange = Math.Round(totalNet - previousNet.Value, 2);
            }

            trialDtos.Add(new TrialExamListItemDto
            {
                Id = trial.Id,
                Name = trial.Name,
                ExamType = trial.ExamType,
                ExamDate = trial.ExamDate,
                TotalNet = Math.Round(totalNet, 2),
                NetChange = netChange
            });

            previousNet = totalNet;
        }

        // Sonuçları en yeni en üstte olacak şekilde sırala
        trialDtos = trialDtos.OrderByDescending(t => t.ExamDate).ToList();

        var stats = await GetTrialExamStatsAsync(userId, examType);

        return new TrialExamListResponse
        {
            Trials = trialDtos,
            Stats = stats
        };
    }

    /// <summary>
    /// Kullanıcının deneme istatistiklerini getirir
    /// </summary>
    public async Task<TrialExamStatsDto> GetTrialExamStatsAsync(int userId, string? examType = null)
    {
        var query = _db.TrialExams
            .Include(t => t.SubjectScores)
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(examType))
            query = query.Where(t => t.ExamType == examType);

        var trials = await query
            .OrderByDescending(t => t.ExamDate)
            .ToListAsync();

        if (!trials.Any())
        {
            return new TrialExamStatsDto
            {
                TotalTrials = 0,
                AverageNet = 0,
                HighestNet = 0,
                LastChange = null,
                LastTrialDate = null
            };
        }

        var netScores = trials.Select(t => t.SubjectScores.Sum(s => s.CorrectAnswers - (s.WrongAnswers / 4.0))).ToList();

        double? lastChange = null;
        if (trials.Count >= 2)
        {
            var orderedByDate = trials.OrderBy(t => t.ExamDate).ToList();
            var lastTrial = orderedByDate.Last();
            var previousTrial = orderedByDate[orderedByDate.Count - 2];

            var lastNet = lastTrial.SubjectScores.Sum(s => s.CorrectAnswers - (s.WrongAnswers / 4.0));
            var previousNet = previousTrial.SubjectScores.Sum(s => s.CorrectAnswers - (s.WrongAnswers / 4.0));

            lastChange = Math.Round(lastNet - previousNet, 2);
        }

        return new TrialExamStatsDto
        {
            TotalTrials = trials.Count,
            AverageNet = Math.Round(netScores.Average(), 2),
            HighestNet = Math.Round(netScores.Max(), 2),
            LastChange = lastChange,
            LastTrialDate = trials.First().ExamDate
        };
    }

    #region Private Helper Methods

    private async Task<double> CalculateTotalNetAsync(int trialExamId)
    {
        var scores = await _db.TrialSubjectScores
            .Where(s => s.TrialExamId == trialExamId)
            .ToListAsync();

        return scores.Sum(s => s.CorrectAnswers - (s.WrongAnswers / 4.0));
    }

    private static TrialExamDto MapToDto(TrialExam trialExam)
    {
        var totalNet = trialExam.SubjectScores.Sum(s => s.CorrectAnswers - (s.WrongAnswers / 4.0));

        return new TrialExamDto
        {
            Id = trialExam.Id,
            Name = trialExam.Name,
            ExamType = trialExam.ExamType,
            ExamDate = trialExam.ExamDate,
            TotalNet = Math.Round(totalNet, 2),
            Ranking = trialExam.Ranking,
            DurationMinutes = trialExam.DurationMinutes,
            Notes = trialExam.Notes,
            CreatedAt = trialExam.CreatedAt,
            SubjectScores = trialExam.SubjectScores.Select(s => new SubjectScoreDto
            {
                Id = s.Id,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject?.Name ?? "Bilinmeyen",
                SubjectShortName = s.Subject?.ShortName ?? "?",
                CorrectAnswers = s.CorrectAnswers,
                WrongAnswers = s.WrongAnswers,
                EmptyAnswers = s.EmptyAnswers,
                NetScore = Math.Round(s.CorrectAnswers - (s.WrongAnswers / 4.0), 2)
            }).ToList()
        };
    }

    #endregion
}

