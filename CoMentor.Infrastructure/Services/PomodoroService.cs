using Microsoft.EntityFrameworkCore;
using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services;

public class PomodoroService : IPomodoroService
{
    private readonly AppDbContext _db;
    private readonly ILeagueService _leagueService;

    // XP hesaplama sabitleri
    private const int XP_PER_MINUTE = 2; // Her dakika için 2 XP
    private const int COMPLETION_BONUS = 10; // Tamamlama bonusu

    private static readonly string[] DayNames = { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };

    public PomodoroService(AppDbContext db, ILeagueService leagueService)
    {
        _db = db;
        _leagueService = leagueService;
    }

    #region Pomodoro Session

    public async Task<PomodoroSessionDto?> StartPomodoroAsync(int userId, StartPomodoroRequest request)
    {
        // Kullanıcı kontrolü
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return null;

        // Aktif oturum kontrolü
        var activeSession = await _db.PomodoroSessions
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsCompleted && p.EndTime == null);

        if (activeSession != null)
            return null; // Zaten aktif bir oturum var

        var session = new PomodoroSession
        {
            UserId = userId,
            SubjectId = request.SubjectId,
            ScheduleId = request.ScheduleId,
            SessionType = request.SessionType,
            PlannedDurationMinutes = request.PlannedDurationMinutes,
            StartTime = DateTime.UtcNow,
            IsCompleted = false,
            Notes = request.Notes,
            XpEarned = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.PomodoroSessions.Add(session);
        await _db.SaveChangesAsync();

        return await GetPomodoroByIdAsync(session.Id);
    }

    public async Task<PomodoroSessionDto?> CompletePomodoroAsync(int userId, int sessionId, CompletePomodoroRequest? request = null)
    {
        var session = await _db.PomodoroSessions
            .Include(p => p.Subject)
            .FirstOrDefaultAsync(p => p.Id == sessionId && p.UserId == userId);

        if (session == null || session.IsCompleted)
            return null;

        // Süreyi hesapla
        var endTime = DateTime.UtcNow;
        var actualMinutes = request?.ActualDurationMinutes
            ?? (int)(endTime - session.StartTime).TotalMinutes;

        // Minimum 1 dakika
        actualMinutes = Math.Max(1, actualMinutes);

        // XP hesapla (sadece STUDY oturumları için)
        int xpEarned = 0;
        if (session.SessionType == "STUDY")
        {
            xpEarned = (actualMinutes * XP_PER_MINUTE) + COMPLETION_BONUS;
        }

        // Oturumu güncelle
        session.EndTime = endTime;
        session.ActualDurationMinutes = actualMinutes;
        session.IsCompleted = true;
        session.XpEarned = xpEarned;

        if (request?.Notes != null)
            session.Notes = request.Notes;

        // XP kazandır
        if (xpEarned > 0)
        {
            // XP transaction oluştur
            var xpTransaction = new XpTransaction
            {
                UserId = userId,
                Amount = xpEarned,
                SourceType = "POMODORO",
                SourceId = session.Id,
                Description = $"{actualMinutes} dakika {session.Subject?.Name ?? "çalışma"} - Pomodoro tamamlandı",
                EarnedAt = DateTime.UtcNow
            };
            _db.XpTransactions.Add(xpTransaction);

            // Kullanıcının toplam XP'sini güncelle
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.TotalXp += xpEarned;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        // Lig kontrolü yap (XP değiştiği için lig yükselmiş olabilir)
        if (xpEarned > 0)
        {
            await _leagueService.CheckAndUpdateUserLeagueAsync(userId);
        }

        return await GetPomodoroByIdAsync(sessionId);
    }

    public async Task<bool> CancelPomodoroAsync(int userId, int sessionId)
    {
        var session = await _db.PomodoroSessions
            .FirstOrDefaultAsync(p => p.Id == sessionId && p.UserId == userId);

        if (session == null || session.IsCompleted)
            return false;

        session.EndTime = DateTime.UtcNow;
        session.IsCompleted = false;
        session.ActualDurationMinutes = 0;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PomodoroSessionDto?> GetActivePomodoroAsync(int userId)
    {
        var session = await _db.PomodoroSessions
            .Include(p => p.Subject)
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsCompleted && p.EndTime == null);

        if (session == null)
            return null;

        return MapToPomodoroDto(session);
    }

    public async Task<List<PomodoroSessionDto>> GetPomodoroHistoryAsync(int userId, int? subjectId = null, int limit = 50)
    {
        var query = _db.PomodoroSessions
            .Include(p => p.Subject)
            .Where(p => p.UserId == userId && p.IsCompleted);

        if (subjectId.HasValue)
            query = query.Where(p => p.SubjectId == subjectId);

        var sessions = await query
            .OrderByDescending(p => p.StartTime)
            .Take(limit)
            .ToListAsync();

        return sessions.Select(MapToPomodoroDto).ToList();
    }

    public async Task<PomodoroStatsDto> GetPomodoroStatsAsync(int userId)
    {
        var sessions = await _db.PomodoroSessions
            .Where(p => p.UserId == userId && p.SessionType == "STUDY")
            .ToListAsync();

        var completedSessions = sessions.Where(s => s.IsCompleted).ToList();
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var todaySessions = completedSessions.Where(s => s.StartTime.Date == today).ToList();
        var weekSessions = completedSessions.Where(s => s.StartTime.Date >= weekStart).ToList();

        return new PomodoroStatsDto
        {
            TotalSessions = sessions.Count,
            CompletedSessions = completedSessions.Count,
            TotalMinutesStudied = completedSessions.Sum(s => s.ActualDurationMinutes ?? 0),
            TotalXpEarned = completedSessions.Sum(s => s.XpEarned),
            TodayMinutes = todaySessions.Sum(s => s.ActualDurationMinutes ?? 0),
            TodaySessions = todaySessions.Count,
            WeekMinutes = weekSessions.Sum(s => s.ActualDurationMinutes ?? 0),
            AverageSessionMinutes = completedSessions.Any()
                ? Math.Round(completedSessions.Average(s => s.ActualDurationMinutes ?? 0), 1)
                : 0
        };
    }

    #endregion

    #region Study Schedule

    public async Task<StudyScheduleDto?> CreateScheduleAsync(int userId, CreateScheduleRequest request)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return null;

        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == request.SubjectId);
        if (!subjectExists)
            return null;

        if (!TimeOnly.TryParse(request.StartTime, out var startTime) ||
            !TimeOnly.TryParse(request.EndTime, out var endTime))
            return null;

        var schedule = new StudySchedule
        {
            UserId = userId,
            SubjectId = request.SubjectId,
            DayOfWeek = request.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            Topic = request.Topic,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.StudySchedules.Add(schedule);
        await _db.SaveChangesAsync();

        return await GetScheduleByIdAsync(schedule.Id);
    }

    public async Task<StudyScheduleDto?> UpdateScheduleAsync(int userId, int scheduleId, UpdateScheduleRequest request)
    {
        var schedule = await _db.StudySchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.UserId == userId);

        if (schedule == null)
            return null;

        if (request.SubjectId.HasValue)
            schedule.SubjectId = request.SubjectId.Value;

        if (request.DayOfWeek.HasValue)
            schedule.DayOfWeek = request.DayOfWeek.Value;

        if (!string.IsNullOrEmpty(request.StartTime) && TimeOnly.TryParse(request.StartTime, out var startTime))
            schedule.StartTime = startTime;

        if (!string.IsNullOrEmpty(request.EndTime) && TimeOnly.TryParse(request.EndTime, out var endTime))
            schedule.EndTime = endTime;

        if (request.Topic != null)
            schedule.Topic = request.Topic;

        if (request.IsActive.HasValue)
            schedule.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return await GetScheduleByIdAsync(scheduleId);
    }

    public async Task<bool> DeleteScheduleAsync(int userId, int scheduleId)
    {
        var schedule = await _db.StudySchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.UserId == userId);

        if (schedule == null)
            return false;

        _db.StudySchedules.Remove(schedule);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<DailyScheduleDto>> GetWeeklyScheduleAsync(int userId)
    {
        var schedules = await _db.StudySchedules
            .Include(s => s.Subject)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var weeklySchedule = new List<DailyScheduleDto>();

        for (int day = 0; day < 7; day++)
        {
            var daySchedules = schedules.Where(s => s.DayOfWeek == day).ToList();

            weeklySchedule.Add(new DailyScheduleDto
            {
                DayOfWeek = day,
                DayName = DayNames[day],
                Schedules = daySchedules.Select(MapToScheduleDto).ToList(),
                TotalMinutes = daySchedules.Sum(s => (int)(s.EndTime - s.StartTime).TotalMinutes)
            });
        }

        return weeklySchedule;
    }

    public async Task<DailyScheduleDto> GetTodayScheduleAsync(int userId)
    {
        var today = (int)DateTime.UtcNow.DayOfWeek;

        var schedules = await _db.StudySchedules
            .Include(s => s.Subject)
            .Where(s => s.UserId == userId && s.IsActive && s.DayOfWeek == today)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        return new DailyScheduleDto
        {
            DayOfWeek = today,
            DayName = DayNames[today],
            Schedules = schedules.Select(MapToScheduleDto).ToList(),
            TotalMinutes = schedules.Sum(s => (int)(s.EndTime - s.StartTime).TotalMinutes)
        };
    }

    #endregion

    #region Private Helper Methods

    private async Task<PomodoroSessionDto?> GetPomodoroByIdAsync(int sessionId)
    {
        var session = await _db.PomodoroSessions
            .Include(p => p.Subject)
            .FirstOrDefaultAsync(p => p.Id == sessionId);

        if (session == null)
            return null;

        return MapToPomodoroDto(session);
    }

    private async Task<StudyScheduleDto?> GetScheduleByIdAsync(int scheduleId)
    {
        var schedule = await _db.StudySchedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null)
            return null;

        return MapToScheduleDto(schedule);
    }

    private static PomodoroSessionDto MapToPomodoroDto(PomodoroSession session)
    {
        return new PomodoroSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            SubjectId = session.SubjectId,
            SubjectName = session.Subject?.Name,
            ScheduleId = session.ScheduleId,
            SessionType = session.SessionType,
            PlannedDurationMinutes = session.PlannedDurationMinutes,
            ActualDurationMinutes = session.ActualDurationMinutes,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            IsCompleted = session.IsCompleted,
            Notes = session.Notes,
            XpEarned = session.XpEarned,
            CreatedAt = session.CreatedAt
        };
    }

    private static StudyScheduleDto MapToScheduleDto(StudySchedule schedule)
    {
        return new StudyScheduleDto
        {
            Id = schedule.Id,
            SubjectId = schedule.SubjectId,
            SubjectName = schedule.Subject?.Name ?? "Bilinmeyen",
            SubjectShortName = schedule.Subject?.ShortName ?? "?",
            SubjectColorHex = schedule.Subject?.ColorHex,
            DayOfWeek = schedule.DayOfWeek,
            DayName = DayNames[schedule.DayOfWeek],
            StartTime = schedule.StartTime.ToString("HH:mm"),
            EndTime = schedule.EndTime.ToString("HH:mm"),
            DurationMinutes = (int)(schedule.EndTime - schedule.StartTime).TotalMinutes,
            Topic = schedule.Topic,
            IsActive = schedule.IsActive
        };
    }

    #endregion
}

