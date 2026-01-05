namespace CoMentor.Application.DTOs;

public class AchievementDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int? XpRequirement { get; set; }
    public int? StreakRequirement { get; set; }
    public int? StudyHoursRequirement { get; set; }
    public string? BadgeColor { get; set; }
    public bool IsEarned { get; set; } // Kullanıcı bunu kazanmış mı?
    public DateTime? EarnedAt { get; set; } // Kazanıldıysa ne zaman?
}

public class CreateAchievementRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int? XpRequirement { get; set; }
    public int? StreakRequirement { get; set; }
    public int? StudyHoursRequirement { get; set; }
    public string? BadgeColor { get; set; }
}
