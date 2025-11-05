using CoMentor.Domain.Entities;
using CoMentor.Application.DTOs;

namespace CoMentor.Application.Mappers;
public static class UserMapper
{
    public static UserDto ToDto(this User u) => new UserDto
    {
        Id = u.Id,
        Email = u.Email,
        Name = u.Name,
        Surname = u.Surname,
        AvatarUrl = u.AvatarUrl,
        SchoolName = u.SchoolName,
        GradeLevel = u.GradeLevel,
        TargetExam = u.TargetExam,
        CurrentStreak = u.CurrentStreak,
        TotalXp = u.TotalXp,
        DailyGoalMinutes = u.DailyGoalMinutes,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt
    };
}