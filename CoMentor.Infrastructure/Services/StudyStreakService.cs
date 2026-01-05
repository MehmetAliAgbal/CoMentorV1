using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Domain.Entities;
using CoMentor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoMentor.Infrastructure.Services;

public class StudyStreakService : IStudyStreakService
{
    private readonly AppDbContext _context;

    public StudyStreakService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CurrentStreakStatusDto> GetUserStreakStatusAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new Exception("User not found");

        var streaks = await _context.StudyStreaks
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        // Bugün çalışma yapıp yapmadığını kontrol etmek için
        // StudyStreak tablosundaki son kayıta bakabiliriz veya User.LastActivity gibi bir alana.
        // Şimdilik StudyStreak tablosundaki aktif kaydın EndDate'i üzerinden gidelim.
        // Ancak StudyStreak yapısı biraz farklı olabilir.
        // Genelde Streak tablosu: StartDate, EndDate (nullable = devam ediyor).
        
        // StudyStreak.cs Step 12'de: StartDate (DateOnly), EndDate (DateOnly?), CurrentDays (int), IsActive (bool).
        
        var currentActiveStreak = streaks.FirstOrDefault(s => s.IsActive);
        
        // Mantık: Eğer aktif bir streak varsa ve bu streak'in bitiş tarihi (EndDate)
        // BUGÜN ise, kullanıcı bugün çalışma yapmış demektir.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool hasStudiedToday = currentActiveStreak != null && currentActiveStreak.EndDate == today;

        return new CurrentStreakStatusDto
        {
            CurrentStreak = user.CurrentStreak,
            LongestStreak = streaks.Any() ? streaks.Max(s => s.CurrentDays) : 0,
            HasStudiedToday = hasStudiedToday,
            StreakHistory = streaks.Select(s => new StudyStreakDto
            {
                Id = s.Id,
                UserId = s.UserId,
                StartDate = s.StartDate.ToString("yyyy-MM-dd"),
                EndDate = s.EndDate?.ToString("yyyy-MM-dd"),
                CurrentDays = s.CurrentDays,
                IsActive = s.IsActive
            }).ToList()
        };
    }

    public async Task UpdateStreakAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        // Son işlem zamanı takibi olmadığı için bu metodun her çağrıldığında
        // "Bugün işlem yaptı" kabul ediyoruz.
        // Ancak veritabanında "Son Güncelleme" bilgisi olmadan "Dün mü girdi bugün mü" ayrımını zor yaparız.
        // StudyStreak tablosunu kullanalım.

        var activeStreak = await _context.StudyStreaks
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        if (activeStreak == null)
        {
            // Hiç aktif streak yok, yeni başlat
            activeStreak = new StudyStreak
            {
                UserId = userId,
                StartDate = today,
                EndDate = today, // Bugün başladı
                CurrentDays = 1,
                IsActive = true
            };
            _context.StudyStreaks.Add(activeStreak);
            user.CurrentStreak = 1;
        }
        else
        {
            // Aktif streak var.
            // Senaryo: Son giriş tarihi (activeStreak.EndDate) ne zaman?
            
            if (activeStreak.EndDate == today)
            {
                // Bugün zaten girmiş/güncellenmiş. İşlem yok.
            }
            else if (activeStreak.EndDate == yesterday)
            {
                // Dün girmiş, bugün devam ediyor.
                activeStreak.EndDate = today;
                activeStreak.CurrentDays++;
                user.CurrentStreak++;
                
                // XP Ödülü burada verilebilir
                user.TotalXp += 10; // Örnek: Günlük giriş XP'si
            }
            else
            {
                // Dünden daha eski. Zincir kırılmış.
                // Eski streak'i kapat
                activeStreak.IsActive = false;
                // Yeni streak başlat
                var newStreak = new StudyStreak
                {
                    UserId = userId,
                    StartDate = today,
                    EndDate = today,
                    CurrentDays = 1,
                    IsActive = true
                };
                _context.StudyStreaks.Add(newStreak);
                user.CurrentStreak = 1;
            }
        }

        await _context.SaveChangesAsync();
    }
}
