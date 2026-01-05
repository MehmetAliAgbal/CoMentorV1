using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces;

public interface IAchievementService
{
    // Tüm başarımları listeler (kullanıcının kazandıkları işaretlenmiş olarak)
    Task<List<AchievementDto>> GetAchievementsAsync(int userId);
    
    // Kullanıcının sadece kazandığı başarımları listeler
    Task<List<AchievementDto>> GetUserAchievementsAsync(int userId);

    // Yeni bir başarım tanımı ekler (Admin vb. için)
    Task<AchievementDto> CreateAchievementAsync(CreateAchievementRequest request);

    // Belirli bir kullanıcı için başarım kontrolü yapar ve gerekirse verir
    Task CheckAndGrantAchievementsAsync(int userId);
}
