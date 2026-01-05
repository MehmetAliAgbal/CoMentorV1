using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Domain.Entities;
using CoMentor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoMentor.Infrastructure.Services;

public class AchievementService : IAchievementService
{
    private readonly AppDbContext _context;
    private readonly ILeagueService _leagueService; // Lig kontrolü için

    public AchievementService(AppDbContext context, ILeagueService leagueService)
    {
        _context = context;
        _leagueService = leagueService;
    }

    public async Task<List<AchievementDto>> GetAchievementsAsync(int userId)
    {
        var allAchievements = await _context.Achievements.Where(a => a.IsActive).ToListAsync();
        var userAchievements = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToListAsync();

        var dtos = allAchievements.Select(a =>
        {
            var userAchievement = userAchievements.FirstOrDefault(ua => ua.AchievementId == a.Id);
            return new AchievementDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Icon = a.Icon,
                XpRequirement = a.XpRequirement,
                StreakRequirement = a.StreakRequirement,
                StudyHoursRequirement = a.StudyHoursRequirement,
                BadgeColor = a.BadgeColor,
                IsEarned = userAchievement != null,
                EarnedAt = userAchievement?.EarnedAt
            };
        }).ToList();

        return dtos;
    }

    public async Task<List<AchievementDto>> GetUserAchievementsAsync(int userId)
    {
        var userAchievements = await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId && ua.Achievement.IsActive)
            .ToListAsync();

        return userAchievements.Select(ua => new AchievementDto
        {
            Id = ua.Achievement.Id,
            Name = ua.Achievement.Name,
            Description = ua.Achievement.Description,
            Icon = ua.Achievement.Icon,
            XpRequirement = ua.Achievement.XpRequirement,
            StreakRequirement = ua.Achievement.StreakRequirement,
            StudyHoursRequirement = ua.Achievement.StudyHoursRequirement,
            BadgeColor = ua.Achievement.BadgeColor,
            IsEarned = true,
            EarnedAt = ua.EarnedAt
        }).ToList();
    }

    public async Task<AchievementDto> CreateAchievementAsync(CreateAchievementRequest request)
    {
        var achievement = new Achievement
        {
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            XpRequirement = request.XpRequirement,
            StreakRequirement = request.StreakRequirement,
            StudyHoursRequirement = request.StudyHoursRequirement,
            BadgeColor = request.BadgeColor,
            IsActive = true
        };

        _context.Achievements.Add(achievement);
        await _context.SaveChangesAsync();

        return new AchievementDto
        {
            Id = achievement.Id,
            Name = achievement.Name,
            Description = achievement.Description,
            Icon = achievement.Icon,
            XpRequirement = achievement.XpRequirement,
            StreakRequirement = achievement.StreakRequirement,
            StudyHoursRequirement = achievement.StudyHoursRequirement,
            BadgeColor = achievement.BadgeColor,
            IsEarned = false
        };
    }

    public async Task CheckAndGrantAchievementsAsync(int userId)
    {
        // Gerekli verileri (User, TrialExams vs.) Include ile çek
        var user = await _context.Users
            .Include(u => u.UserAchievements)
            .Include(u => u.TrialExams)
            .Include(u => u.PomodoroSessions)
            .Include(u => u.DailyGoals) 
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        // Henüz seed edilmemişse seed et (Normalde Program.cs'de veya migration'da yapılır ama burası garanti olsun)
        await SeedAchievementsAsync();

        var earnedIds = user.UserAchievements.Select(ua => ua.AchievementId).ToList();
        var potentialAchievements = await _context.Achievements
            .Where(a => a.IsActive && !earnedIds.Contains(a.Id))
            .ToListAsync();
        
        // Lig bilgisini al (Elmas ligi vs kontrolü için)
        // Performans notu: Her çağrıda League servisine gitmek pahalı olabilir ama şimdilik kabul edilebilir.
        var userLeagueDto = await _leagueService.GetUserLeagueAsync(userId);
        var currentLeagueName = userLeagueDto?.CurrentLeague?.Name;
        // Haftalık sıralama (Weekly Rank) için basitçe ligdeki sırasına bakıyoruz (Haftalık reset mantığı ayrı konu)
        var weeklyRank = userLeagueDto?.RankInLeague;

        foreach (var achievement in potentialAchievements)
        {
            bool earned = false;

            // 1. Genel Xp/Streak Kontrolleri (Basit)
            if (achievement.XpRequirement.HasValue && user.TotalXp >= achievement.XpRequirement.Value) earned = true;
            if (achievement.StreakRequirement.HasValue && user.CurrentStreak >= achievement.StreakRequirement.Value) earned = true;
            
            // 2. Özel İsim Bazlı Logic Kontrolleri (Karmaşık)
            // Bu yöntem hard-coded string'lere bağımlıdır ama hızlı çözüm sağlar.
            // Daha sağlam yol: Achievement tablosuna 'Code' veya 'Type' kolonu eklemektir.
            // Şimdilik 'Name' üzerinden gidiyoruz.

            switch (achievement.Name)
            {
                // 🔥 Streak (Seri) Odaklı Başarımlar
                case "Isınma Turları": // Haftalık Seri, 7 gün
                    if (user.CurrentStreak >= 7) earned = true;
                    break;
                case "Kamp Ateşi": // Aylık Seri, 30 gün
                    if (user.CurrentStreak >= 30) earned = true;
                    break;
                case "Efsanevi İrade": // Mevsimlik Seri, 90 gün
                    if (user.CurrentStreak >= 90) earned = true;
                    break;
                case "İstikrarlı Maratoncu": // 30 gün
                    if (user.CurrentStreak >= 30) earned = true;
                    break;

                // 💯 100 Kulübü (TYT 100 Net/Puan)
                case "100 Kulübü":
                    if (user.TrialExams.Any(t => t.ExamType == "TYT" && t.TotalScore >= 100)) earned = true;
                    break;

                // ⏱️ Odak Ustası (50 saat = 3000 dk Pomodoro)
                case "Odak Ustası":
                    var totalStudyMinutes = user.PomodoroSessions
                        .Where(p => p.IsCompleted && p.SessionType == "STUDY")
                        .Sum(p => p.ActualDurationMinutes ?? 0);
                    if (totalStudyMinutes >= 3000) earned = true; // 50 saat
                    break;

                // 📚 Deneme Canavarı (20 farklı deneme)
                case "Deneme Canavarı":
                    if (user.TrialExams.Count >= 20) earned = true;
                    break;

                // 🦉 Gece Kuşu (22:00 - 04:00 arası çalışma)
                // Koşul: Tamamlanan son study_session saati 22:00-04:00 arasında.
                case "Gece Kuşu":
                    if (user.PomodoroSessions.Any(p => p.IsCompleted && IsNightOwlTime(p.EndTime ?? p.StartTime))) earned = true;
                    break;

                // 📈 Durmak Yok (Son 3 denemede artış)
                case "Durmak Yok":
                    earned = CheckConsistentGrowth(user.TrialExams);
                    break;

                // 🌅 Erkenci Tayfa (06:00 - 08:00 arası başlatma)
                case "Erkenci Tayfa":
                    if (user.PomodoroSessions.Any(p => IsEarlyBirdTime(p.StartTime))) earned = true;
                    break;

                // 🔥 Haftanın Yıldızı (İlk 3)
                case "Haftanın Yıldızı":
                    if (weeklyRank.HasValue && weeklyRank.Value <= 3) earned = true;
                    break;

                // 📝 Konu Ekspertizi (Günde 360 dk çalışma)
                case "Konu Ekspertizi":
                    earned = CheckDailyStudyRecord(user.PomodoroSessions, 360);
                    break;

                // 💎 Elmas Ligi
                case "Elmas Ligi":
                    if (currentLeagueName == "Diamond" || currentLeagueName == "Elmas") earned = true;
                    break;
            }

            if (earned)
            {
                _context.UserAchievements.Add(new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievement.Id,
                    EarnedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    // --- Helpers for Logic ---

    private bool IsNightOwlTime(DateTime time)
    {
        // 22:00 - 04:00 arası.
        // UTC veya Local farkına dikkat edilmeli. Şimdilik time üzerinden saat kontrolü yapıyoruz.
        // Veritabanında UTC dönüyorsa +3 eklemek gerekebilir.
        // Basitlik adına saati 22,23,0,1,2,3 olanlar diyelim.
        var hour = time.Hour; 
        // Türkiye saati dönüşümü (eğer server UTC ise)
        // Ama time parametresi zaten local geliyorsa sorun yok.
        // Biz burada UTC + 3 varsayımıyla (veya kullanıcı local saati) kontrol edelim.
        // Daha güvenli yol: time.AddHours(3).Hour (Eğer UTC ise)
        var localHour = time.AddHours(3).Hour;
        return localHour >= 22 || localHour < 4;
    }

    private bool IsEarlyBirdTime(DateTime time)
    {
        // 06:00 - 08:00
        var localHour = time.AddHours(3).Hour;
        return localHour >= 6 && localHour < 8;
    }

    private bool CheckConsistentGrowth(ICollection<TrialExam> trials)
    {
        if (trials.Count < 3) return false;
        
        var sortedTrials = trials.OrderByDescending(t => t.ExamDate).Take(3).ToList();
        // sortedTrials[0] = En yeni
        // sortedTrials[1] = Orta
        // sortedTrials[2] = En eski
        
        // net3 > net2 > net1  (En yeni > Orta > En eski)
        return sortedTrials[0].TotalScore > sortedTrials[1].TotalScore && 
               sortedTrials[1].TotalScore > sortedTrials[2].TotalScore;
    }

    private bool CheckDailyStudyRecord(ICollection<PomodoroSession> sessions, int targetMinutes)
    {
        // Herhangi bir günde toplam çalışma süresi targetMinutes'i geçti mi?
        var dailyTotals = sessions
            .Where(p => p.IsCompleted)
            .GroupBy(p => p.StartTime.Date)
            .Select(g => g.Sum(p => p.ActualDurationMinutes ?? 0));
            
        return dailyTotals.Any(total => total >= targetMinutes);
    }

    // --- Seeding ---
    
    private async Task SeedAchievementsAsync()
    {
        if (await _context.Achievements.AnyAsync()) return; // Zaten dolu

        var list = new List<Achievement>
        {
            new() { Name = "Isınma Turları", Description = "7 gün üst üste hiç aksatmadan uygulamaya giriş yap.", StreakRequirement = 7, Icon = "⚡", BadgeColor = "#FFC107" },
            new() { Name = "Kamp Ateşi", Description = "Tam 1 ay (30 gün) boyunca serini bozmadan devam ettir.", StreakRequirement = 30, Icon = "🏔️", BadgeColor = "#FF5722" },
            new() { Name = "Efsanevi İrade", Description = "90 gün boyunca her gün çalışarak sarsılmaz bir disiplin göster.", StreakRequirement = 90, Icon = "👑", BadgeColor = "#9C27B0" },
            new() { Name = "İstikrarlı Maratoncu", Description = "30 gün boyunca hiç gün aksatmadan uygulamaya giriş yap.", StreakRequirement = 30, Icon = "🚀", BadgeColor = "#2196F3" },
            new() { Name = "100 Kulübü", Description = "Bir TYT denemesinde 100 veya üzeri net/puan yap.", Icon = "💯", BadgeColor = "#f44336" },
            new() { Name = "Odak Ustası", Description = "Toplamda 50 saatlik Pomodoro çalışmasını tamamla.", StudyHoursRequirement = 50, Icon = "⏱️", BadgeColor = "#607D8B" },
            new() { Name = "Deneme Canavarı", Description = "Toplamda 20 farklı deneme sınavı sonucu gir.", Icon = "📚", BadgeColor = "#795548" },
            new() { Name = "Gece Kuşu", Description = "Gece 22:00 ile sabah 04:00 arasında bir çalışma oturumu tamamla.", Icon = "🦉", BadgeColor = "#3F51B5" },
            new() { Name = "Durmak Yok", Description = "Son 3 denemede netlerini sürekli artır.", Icon = "📈", BadgeColor = "#4CAF50" },
            new() { Name = "Erkenci Tayfa", Description = "Sabah 06:00 - 08:00 arasında bir çalışma seansı başlat.", Icon = "🌅", BadgeColor = "#FF9800" },
            new() { Name = "Haftanın Yıldızı", Description = "Kendi liginde haftayı ilk 3'te tamamla.", Icon = "🔥", BadgeColor = "#E91E63" },
            new() { Name = "Konu Ekspertizi", Description = "Tek bir günde 6 saatten (360 dk) fazla konu çalışması yap.", Icon = "📝", BadgeColor = "#009688" },
            new() { Name = "Elmas Ligi", Description = "En üst lig olan Elmas Ligi'ne yüksel.", Icon = "💎", BadgeColor = "#00BCD4" }
        };

        _context.Achievements.AddRange(list);
        await _context.SaveChangesAsync();
    }
}
