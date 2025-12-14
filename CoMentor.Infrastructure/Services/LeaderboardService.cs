using Microsoft.EntityFrameworkCore;
using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _db;
    private readonly ILeagueService _leagueService;

    public LeaderboardService(AppDbContext db, ILeagueService leagueService)
    {
        _db = db;
        _leagueService = leagueService;
    }

    #region Leaderboard

    /// <summary>
    /// Genel sıralama - Tüm kullanıcılar
    /// </summary>
    public async Task<LeaderboardResponse> GetGeneralLeaderboardAsync(int? currentUserId = null, int limit = 100)
    {
        var users = await _db.Users
            .OrderByDescending(u => u.TotalXp)
            .ThenByDescending(u => u.CurrentStreak)
            .Take(limit)
            .ToListAsync();

        var rankings = await BuildRankingsWithLeagueAsync(users, currentUserId);
        var currentUserRank = await GetCurrentUserRankAsync(currentUserId, rankings, null, null);

        return new LeaderboardResponse
        {
            LeagueType = "General",
            LeagueName = "Genel Sıralama",
            Rankings = rankings,
            CurrentUserRank = currentUserRank,
            TotalUsers = await _db.Users.CountAsync()
        };
    }

    /// <summary>
    /// Okul ligi - Aynı okuldaki kullanıcılar
    /// </summary>
    public async Task<LeaderboardResponse> GetSchoolLeaderboardAsync(int userId, int limit = 100)
    {
        var currentUser = await _db.Users.FindAsync(userId);
        if (currentUser == null || string.IsNullOrEmpty(currentUser.SchoolName))
        {
            return new LeaderboardResponse
            {
                LeagueType = "School",
                LeagueName = "Okul Ligi",
                Rankings = new List<LeaderboardItemDto>(),
                TotalUsers = 0
            };
        }

        var schoolName = currentUser.SchoolName;

        var users = await _db.Users
            .Where(u => u.SchoolName == schoolName)
            .OrderByDescending(u => u.TotalXp)
            .ThenByDescending(u => u.CurrentStreak)
            .Take(limit)
            .ToListAsync();

        var rankings = await BuildRankingsWithLeagueAsync(users, userId);
        var currentUserRank = await GetCurrentUserRankAsync(userId, rankings, schoolName, null);

        return new LeaderboardResponse
        {
            LeagueType = "School",
            LeagueName = $"{schoolName} Ligi",
            Rankings = rankings,
            CurrentUserRank = currentUserRank,
            TotalUsers = await _db.Users.CountAsync(u => u.SchoolName == schoolName)
        };
    }

    /// <summary>
    /// Sınıf ligi - Aynı sınıftaki kullanıcılar
    /// </summary>
    public async Task<LeaderboardResponse> GetGradeLeaderboardAsync(int userId, int limit = 100)
    {
        var currentUser = await _db.Users.FindAsync(userId);
        if (currentUser == null || !currentUser.GradeLevel.HasValue)
        {
            return new LeaderboardResponse
            {
                LeagueType = "Grade",
                LeagueName = "Sınıf Ligi",
                Rankings = new List<LeaderboardItemDto>(),
                TotalUsers = 0
            };
        }

        var gradeLevel = currentUser.GradeLevel.Value;

        var users = await _db.Users
            .Where(u => u.GradeLevel == gradeLevel)
            .OrderByDescending(u => u.TotalXp)
            .ThenByDescending(u => u.CurrentStreak)
            .Take(limit)
            .ToListAsync();

        var rankings = await BuildRankingsWithLeagueAsync(users, userId);
        var currentUserRank = await GetCurrentUserRankAsync(userId, rankings, null, gradeLevel);

        var gradeName = GetGradeName(gradeLevel);

        return new LeaderboardResponse
        {
            LeagueType = "Grade",
            LeagueName = $"{gradeName} Ligi",
            Rankings = rankings,
            CurrentUserRank = currentUserRank,
            TotalUsers = await _db.Users.CountAsync(u => u.GradeLevel == gradeLevel)
        };
    }

    /// <summary>
    /// Tüm ligleri tek seferde getirir
    /// </summary>
    public async Task<AllLeaguesResponse> GetAllLeaguesAsync(int userId, int limit = 50)
    {
        var general = await GetGeneralLeaderboardAsync(userId, limit);
        var school = await GetSchoolLeaderboardAsync(userId, limit);
        var grade = await GetGradeLeaderboardAsync(userId, limit);

        return new AllLeaguesResponse
        {
            General = general,
            School = school.Rankings.Any() ? school : null,
            Grade = grade.Rankings.Any() ? grade : null
        };
    }

    #endregion

    #region XP

    public async Task<List<XpTransactionDto>> GetXpHistoryAsync(int userId, int limit = 50)
    {
        var transactions = await _db.XpTransactions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.EarnedAt)
            .Take(limit)
            .ToListAsync();

        return transactions.Select(t => new XpTransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            SourceType = t.SourceType,
            Description = t.Description,
            EarnedAt = t.EarnedAt
        }).ToList();
    }

    public async Task<UserXpSummaryDto> GetUserXpSummaryAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return new UserXpSummaryDto { UserId = userId };
        }

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var transactions = await _db.XpTransactions
            .Where(x => x.UserId == userId)
            .ToListAsync();

        var todayXp = transactions.Where(x => x.EarnedAt.Date == today).Sum(x => x.Amount);
        var weekXp = transactions.Where(x => x.EarnedAt.Date >= weekStart).Sum(x => x.Amount);
        var monthXp = transactions.Where(x => x.EarnedAt.Date >= monthStart).Sum(x => x.Amount);

        // Genel sıralama
        var generalRank = await _db.Users.CountAsync(u => u.TotalXp > user.TotalXp) + 1;
        var totalUsers = await _db.Users.CountAsync();

        // Okul sıralaması
        SchoolRankDto? schoolRank = null;
        if (!string.IsNullOrEmpty(user.SchoolName))
        {
            var schoolRankNum = await _db.Users.CountAsync(u => u.SchoolName == user.SchoolName && u.TotalXp > user.TotalXp) + 1;
            var schoolUsers = await _db.Users.CountAsync(u => u.SchoolName == user.SchoolName);
            schoolRank = new SchoolRankDto
            {
                Rank = schoolRankNum,
                TotalUsers = schoolUsers,
                SchoolName = user.SchoolName
            };
        }

        // Sınıf sıralaması
        GradeRankDto? gradeRank = null;
        if (user.GradeLevel.HasValue)
        {
            var gradeRankNum = await _db.Users.CountAsync(u => u.GradeLevel == user.GradeLevel && u.TotalXp > user.TotalXp) + 1;
            var gradeUsers = await _db.Users.CountAsync(u => u.GradeLevel == user.GradeLevel);
            gradeRank = new GradeRankDto
            {
                Rank = gradeRankNum,
                TotalUsers = gradeUsers,
                GradeLevel = user.GradeLevel.Value,
                GradeName = GetGradeName(user.GradeLevel.Value)
            };
        }

        // Lig bilgisi
        UserLeagueInfoDto? leagueInfo = null;
        var userLeague = await _leagueService.GetUserLeagueAsync(userId);
        if (userLeague != null)
        {
            leagueInfo = new UserLeagueInfoDto
            {
                LeagueId = userLeague.CurrentLeague.Id,
                LeagueName = userLeague.CurrentLeague.Name,
                LeagueIcon = userLeague.CurrentLeague.Icon,
                LeagueColor = userLeague.CurrentLeague.LeagueColor,
                RankInLeague = userLeague.RankInLeague,
                TotalUsersInLeague = userLeague.TotalUsersInLeague,
                XpToNextLeague = userLeague.XpToNextLeague,
                ProgressPercentage = userLeague.ProgressPercentage,
                NextLeagueName = userLeague.NextLeague?.Name
            };
        }

        return new UserXpSummaryDto
        {
            UserId = userId,
            TotalXp = user.TotalXp,
            TodayXp = todayXp,
            WeekXp = weekXp,
            MonthXp = monthXp,
            CurrentStreak = user.CurrentStreak,
            GeneralRank = new GeneralRankDto { Rank = generalRank, TotalUsers = totalUsers },
            SchoolRank = schoolRank,
            GradeRank = gradeRank,
            LeagueInfo = leagueInfo
        };
    }

    #endregion

    #region Private Helper Methods

    private static string GetGradeName(int gradeLevel)
    {
        return gradeLevel switch
        {
            9 => "9. Sınıf",
            10 => "10. Sınıf",
            11 => "11. Sınıf",
            12 => "12. Sınıf",
            _ => $"{gradeLevel}. Sınıf"
        };
    }

    private async Task<List<LeaderboardItemDto>> BuildRankingsWithLeagueAsync(List<User> users, int? currentUserId)
    {
        var rankings = new List<LeaderboardItemDto>();
        int rank = 1;

        // Tüm ligleri önbelleğe al
        var leagues = await _db.Leagues.ToListAsync();

        foreach (var user in users)
        {
            // Kullanıcının ligini bul
            var userLeague = leagues
                .Where(l => l.MinXp <= user.TotalXp && (l.MaxXp == null || l.MaxXp >= user.TotalXp))
                .OrderByDescending(l => l.RankOrder)
                .FirstOrDefault();

            rankings.Add(new LeaderboardItemDto
            {
                Rank = rank,
                UserId = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                AvatarUrl = user.AvatarUrl,
                SchoolName = user.SchoolName,
                GradeLevel = user.GradeLevel,
                TotalXp = user.TotalXp,
                CurrentStreak = user.CurrentStreak,
                IsCurrentUser = currentUserId.HasValue && user.Id == currentUserId.Value,
                // Lig bilgileri
                LeagueId = userLeague?.Id,
                LeagueName = userLeague?.Name,
                LeagueIcon = userLeague?.Icon,
                LeagueColor = userLeague?.LeagueColor
            });
            rank++;
        }

        return rankings;
    }

    private async Task<LeaderboardItemDto?> GetCurrentUserRankAsync(
        int? currentUserId,
        List<LeaderboardItemDto> rankings,
        string? schoolName,
        int? gradeLevel)
    {
        if (!currentUserId.HasValue)
            return null;

        var currentUserRank = rankings.FirstOrDefault(r => r.UserId == currentUserId.Value);

        if (currentUserRank != null)
            return currentUserRank;

        var user = await _db.Users.FindAsync(currentUserId.Value);
        if (user == null)
            return null;

        var userXp = user.TotalXp;
        int userRank;

        if (!string.IsNullOrEmpty(schoolName))
        {
            userRank = await _db.Users.CountAsync(u => u.SchoolName == schoolName && u.TotalXp > userXp);
        }
        else if (gradeLevel.HasValue)
        {
            userRank = await _db.Users.CountAsync(u => u.GradeLevel == gradeLevel && u.TotalXp > userXp);
        }
        else
        {
            userRank = await _db.Users.CountAsync(u => u.TotalXp > userXp);
        }

        // Kullanıcının ligini bul
        var userLeague = await _db.Leagues
            .Where(l => l.MinXp <= user.TotalXp && (l.MaxXp == null || l.MaxXp >= user.TotalXp))
            .OrderByDescending(l => l.RankOrder)
            .FirstOrDefaultAsync();

        return new LeaderboardItemDto
        {
            Rank = userRank + 1,
            UserId = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            AvatarUrl = user.AvatarUrl,
            SchoolName = user.SchoolName,
            GradeLevel = user.GradeLevel,
            TotalXp = user.TotalXp,
            CurrentStreak = user.CurrentStreak,
            IsCurrentUser = true,
            // Lig bilgileri
            LeagueId = userLeague?.Id,
            LeagueName = userLeague?.Name,
            LeagueIcon = userLeague?.Icon,
            LeagueColor = userLeague?.LeagueColor
        };
    }

    #endregion
}

