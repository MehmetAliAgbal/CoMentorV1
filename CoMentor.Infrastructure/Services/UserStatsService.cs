using CoMentor.Application.DTOs;
using CoMentor.Application.Interfaces;
using CoMentor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class UserStatsService : IUserStatsService
{
    private readonly AppDbContext _context;

    public UserStatsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserStatsDto?> GetUserStatsAsync(int userId)
    {
        return await _context.UserStats
            .Where(u => u.Id == userId)
            .Select(u => new UserStatsDto
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                TotalXp = u.TotalXp,
                CurrentStreak = u.CurrentStreak,
                CurrentLeague = u.CurrentLeague,
                TotalTrials = u.TotalTrials,
                AvgTrialScore = u.AvgTrialScore,
                TotalStudyMinutes = u.TotalStudyMinutes
            })
            .FirstOrDefaultAsync();
    }
}
