using Microsoft.EntityFrameworkCore;
using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Infrastructure.Persistence;

namespace CoMentor.Infrastructure.Services;

public class LeagueService : ILeagueService
{
    private readonly AppDbContext _db;

    public LeagueService(AppDbContext db)
    {
        _db = db;
    }

    #region Lig Bilgileri

    /// <summary>
    /// Tüm ligleri getirir (sıralı)
    /// </summary>
    public async Task<List<LeagueDto>> GetAllLeaguesAsync()
    {
        var leagues = await _db.Leagues
            .OrderBy(l => l.RankOrder)
            .ToListAsync();

        return leagues.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Belirli bir ligi getirir
    /// </summary>
    public async Task<LeagueDto?> GetLeagueByIdAsync(int leagueId)
    {
        var league = await _db.Leagues.FindAsync(leagueId);
        return league != null ? MapToDto(league) : null;
    }

    /// <summary>
    /// XP'ye göre hangi ligde olduğunu belirler
    /// </summary>
    public async Task<LeagueDto?> GetLeagueByXpAsync(int xp)
    {
        var league = await _db.Leagues
            .Where(l => l.MinXp <= xp && (l.MaxXp == null || l.MaxXp >= xp))
            .OrderByDescending(l => l.RankOrder)
            .FirstOrDefaultAsync();

        return league != null ? MapToDto(league) : null;
    }

    #endregion

    #region Kullanıcı Lig Durumu

    /// <summary>
    /// Kullanıcının mevcut lig durumunu getirir
    /// </summary>
    public async Task<UserLeagueDto?> GetUserLeagueAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        var currentLeague = await GetLeagueByXpAsync(user.TotalXp);
        if (currentLeague == null) return null;

        // Bir sonraki lig
        var nextLeague = await _db.Leagues
            .Where(l => l.RankOrder == currentLeague.RankOrder + 1)
            .FirstOrDefaultAsync();

        // Lig içi sıralama
        var usersInLeague = await GetUsersInLeagueAsync(currentLeague.Id);
        var rankInLeague = usersInLeague
            .OrderByDescending(u => u.TotalXp)
            .ThenByDescending(u => u.CurrentStreak)
            .ToList()
            .FindIndex(u => u.Id == userId) + 1;

        // İlerleme hesaplama
        int xpInCurrentLeague = user.TotalXp - currentLeague.MinXp;
        int xpToNextLeague = 0;
        double progressPercentage = 100;

        if (nextLeague != null)
        {
            xpToNextLeague = nextLeague.MinXp - user.TotalXp;
            int leagueRange = nextLeague.MinXp - currentLeague.MinXp;
            progressPercentage = leagueRange > 0 
                ? Math.Min(100, (double)xpInCurrentLeague / leagueRange * 100) 
                : 100;
        }

        return new UserLeagueDto
        {
            UserId = userId,
            UserName = $"{user.Name} {user.Surname}",
            TotalXp = user.TotalXp,
            CurrentLeague = currentLeague,
            NextLeague = nextLeague != null ? MapToDto(nextLeague) : null,
            XpInCurrentLeague = xpInCurrentLeague,
            XpToNextLeague = xpToNextLeague,
            ProgressPercentage = Math.Round(progressPercentage, 1),
            RankInLeague = rankInLeague,
            TotalUsersInLeague = usersInLeague.Count
        };
    }

    /// <summary>
    /// Kullanıcının lig geçmişini getirir
    /// </summary>
    public async Task<List<UserLeagueHistoryDto>> GetUserLeagueHistoryAsync(int userId)
    {
        var history = await _db.UserLeagueHistories
            .Include(h => h.League)
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.StartDate)
            .ToListAsync();

        return history.Select(h => new UserLeagueHistoryDto
        {
            Id = h.Id,
            League = MapToDto(h.League),
            StartDate = h.StartDate,
            EndDate = h.EndDate,
            FinalXp = h.FinalXp,
            FinalRank = h.FinalRank,
            IsCurrent = h.IsCurrent
        }).ToList();
    }

    #endregion

    #region Lig Sıralaması

    /// <summary>
    /// Belirli bir ligteki kullanıcı sıralamasını getirir
    /// </summary>
    public async Task<LeagueLeaderboardDto?> GetLeagueLeaderboardAsync(int leagueId, int? currentUserId = null, int limit = 100)
    {
        var league = await _db.Leagues.FindAsync(leagueId);
        if (league == null) return null;

        var usersInLeague = await GetUsersInLeagueAsync(leagueId);
        
        var rankedUsers = usersInLeague
            .OrderByDescending(u => u.TotalXp)
            .ThenByDescending(u => u.CurrentStreak)
            .Take(limit)
            .ToList();

        var rankings = new List<LeagueLeaderboardItemDto>();
        int rank = 1;

        foreach (var user in rankedUsers)
        {
            rankings.Add(new LeagueLeaderboardItemDto
            {
                Rank = rank,
                UserId = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                AvatarUrl = user.AvatarUrl,
                TotalXp = user.TotalXp,
                CurrentStreak = user.CurrentStreak,
                IsCurrentUser = currentUserId.HasValue && user.Id == currentUserId.Value
            });
            rank++;
        }

        LeagueLeaderboardItemDto? currentUserRank = null;
        if (currentUserId.HasValue)
        {
            currentUserRank = rankings.FirstOrDefault(r => r.UserId == currentUserId.Value);
            
            // Eğer listede yoksa, tam sıralamasını bul
            if (currentUserRank == null)
            {
                var currentUser = usersInLeague.FirstOrDefault(u => u.Id == currentUserId.Value);
                if (currentUser != null)
                {
                    var fullRank = usersInLeague
                        .OrderByDescending(u => u.TotalXp)
                        .ThenByDescending(u => u.CurrentStreak)
                        .ToList()
                        .FindIndex(u => u.Id == currentUserId.Value) + 1;

                    currentUserRank = new LeagueLeaderboardItemDto
                    {
                        Rank = fullRank,
                        UserId = currentUser.Id,
                        Name = currentUser.Name,
                        Surname = currentUser.Surname,
                        AvatarUrl = currentUser.AvatarUrl,
                        TotalXp = currentUser.TotalXp,
                        CurrentStreak = currentUser.CurrentStreak,
                        IsCurrentUser = true
                    };
                }
            }
        }

        return new LeagueLeaderboardDto
        {
            League = MapToDto(league),
            Rankings = rankings,
            TotalUsers = usersInLeague.Count,
            CurrentUserRank = currentUserRank
        };
    }

    /// <summary>
    /// Tüm liglerin genel görünümünü getirir
    /// </summary>
    public async Task<AllLeaguesOverviewDto> GetAllLeaguesOverviewAsync(int? currentUserId = null)
    {
        var leagues = await _db.Leagues
            .OrderBy(l => l.RankOrder)
            .ToListAsync();

        var allUsers = await _db.Users.ToListAsync();
        
        int? currentUserLeagueId = null;
        if (currentUserId.HasValue)
        {
            var currentUser = allUsers.FirstOrDefault(u => u.Id == currentUserId.Value);
            if (currentUser != null)
            {
                var userLeague = leagues.FirstOrDefault(l => 
                    l.MinXp <= currentUser.TotalXp && 
                    (l.MaxXp == null || l.MaxXp >= currentUser.TotalXp));
                currentUserLeagueId = userLeague?.Id;
            }
        }

        var leagueOverviews = new List<LeagueOverviewItemDto>();

        foreach (var league in leagues)
        {
            var usersInLeague = allUsers.Count(u => 
                u.TotalXp >= league.MinXp && 
                (league.MaxXp == null || u.TotalXp <= league.MaxXp));

            leagueOverviews.Add(new LeagueOverviewItemDto
            {
                League = MapToDto(league),
                TotalUsers = usersInLeague,
                IsCurrentUserLeague = currentUserLeagueId.HasValue && league.Id == currentUserLeagueId.Value
            });
        }

        return new AllLeaguesOverviewDto
        {
            Leagues = leagueOverviews,
            CurrentUserLeagueId = currentUserLeagueId
        };
    }

    #endregion

    #region Lig Güncelleme

    /// <summary>
    /// Kullanıcının ligini kontrol eder ve gerekirse günceller
    /// </summary>
    public async Task<LeagueChangeDto> CheckAndUpdateUserLeagueAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            return new LeagueChangeDto { HasChanged = false };
        }

        var newLeague = await _db.Leagues
            .Where(l => l.MinXp <= user.TotalXp && (l.MaxXp == null || l.MaxXp >= user.TotalXp))
            .OrderByDescending(l => l.RankOrder)
            .FirstOrDefaultAsync();

        if (newLeague == null)
        {
            return new LeagueChangeDto { HasChanged = false };
        }

        // Mevcut aktif lig kaydını bul
        var currentHistory = await _db.UserLeagueHistories
            .Include(h => h.League)
            .Where(h => h.UserId == userId && h.IsCurrent)
            .FirstOrDefaultAsync();

        // Eğer lig değişmediyse
        if (currentHistory != null && currentHistory.LeagueId == newLeague.Id)
        {
            return new LeagueChangeDto { HasChanged = false };
        }

        var previousLeague = currentHistory?.League;

        // Eski kaydı kapat
        if (currentHistory != null)
        {
            currentHistory.IsCurrent = false;
            currentHistory.EndDate = DateTime.UtcNow;
            currentHistory.FinalXp = user.TotalXp;
        }

        // Yeni kayıt oluştur
        var newHistory = new UserLeagueHistory
        {
            UserId = userId,
            LeagueId = newLeague.Id,
            StartDate = DateTime.UtcNow,
            IsCurrent = true
        };

        _db.UserLeagueHistories.Add(newHistory);
        await _db.SaveChangesAsync();

        bool isPromotion = previousLeague == null || newLeague.RankOrder > previousLeague.RankOrder;
        string message = isPromotion
            ? $"Tebrikler! {newLeague.Name} Lige yükseldiniz! 🎉"
            : $"{newLeague.Name} Lige düştünüz.";

        return new LeagueChangeDto
        {
            HasChanged = true,
            PreviousLeague = previousLeague != null ? MapToDto(previousLeague) : null,
            NewLeague = MapToDto(newLeague),
            Message = message,
            IsPromotion = isPromotion
        };
    }

    /// <summary>
    /// Yeni kullanıcı için başlangıç ligini atar
    /// </summary>
    public async Task InitializeUserLeagueAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        // Zaten lig kaydı var mı kontrol et
        var existingHistory = await _db.UserLeagueHistories
            .AnyAsync(h => h.UserId == userId);

        if (existingHistory) return;

        // Başlangıç ligini bul (genellikle en düşük lig)
        var startingLeague = await _db.Leagues
            .OrderBy(l => l.RankOrder)
            .FirstOrDefaultAsync();

        if (startingLeague == null) return;

        var history = new UserLeagueHistory
        {
            UserId = userId,
            LeagueId = startingLeague.Id,
            StartDate = DateTime.UtcNow,
            IsCurrent = true
        };

        _db.UserLeagueHistories.Add(history);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region Private Helpers

    private async Task<List<User>> GetUsersInLeagueAsync(int leagueId)
    {
        var league = await _db.Leagues.FindAsync(leagueId);
        if (league == null) return new List<User>();

        return await _db.Users
            .Where(u => u.TotalXp >= league.MinXp && 
                       (league.MaxXp == null || u.TotalXp <= league.MaxXp))
            .ToListAsync();
    }

    private static LeagueDto MapToDto(League league)
    {
        return new LeagueDto
        {
            Id = league.Id,
            Name = league.Name,
            MinXp = league.MinXp,
            MaxXp = league.MaxXp,
            LeagueColor = league.LeagueColor,
            Icon = league.Icon,
            RankOrder = league.RankOrder
        };
    }

    #endregion
}

