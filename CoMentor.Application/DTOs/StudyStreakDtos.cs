namespace CoMentor.Application.DTOs;

public class StudyStreakDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string StartDate { get; set; } = null!; // DateOnly serialization issues, string olarak dönmek daha güvenli olabilir
    public string? EndDate { get; set; }
    public int CurrentDays { get; set; }
    public bool IsActive { get; set; }
}

public class CurrentStreakStatusDto
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; } // Gerekirse eklenebilir
    public bool HasStudiedToday { get; set; }
    public List<StudyStreakDto> StreakHistory { get; set; } = new();
}
