using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface IUserStatsService
    {
        Task<UserStatsDto?> GetUserStatsAsync(int userId);
    }
}


