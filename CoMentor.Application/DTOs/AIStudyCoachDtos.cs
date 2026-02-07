using System.Collections.Generic;

namespace CoMentor.Application.DTOs;

public class GenerateScheduleRequestDto
{
    // Eski basit kullanım için opsiyonel
    public int? TargetWeeklyHours { get; set; }
    
    // UI'dan gelen detaylı veriler
    public List<DailyAvailabilityDto> WeeklyAvailability { get; set; } = new();
    public List<SubjectPreferenceDto> SubjectPreferences { get; set; } = new();
}

public class DailyAvailabilityDto
{
    public int DayOfWeek { get; set; } // 0=Pazar, 1=Pazartesi...
    
    // "09:00", "10:00" gibi seçili saatlerin başlangıçları
    public List<string> TimeSlots { get; set; } = new(); 
}

public class SubjectPreferenceDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    
    // Kullanıcının özellikle çalışmak istediği konular
    public List<string> SelectedTopics { get; set; } = new();
}
