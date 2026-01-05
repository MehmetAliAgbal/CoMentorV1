using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces;

public interface IStudyStreakService
{
    // Kullanıcının güncel streak durumunu ve geçmişini getirir
    Task<CurrentStreakStatusDto> GetUserStreakStatusAsync(int userId);

    // Streak günceller (Login veya işlem sonrası çağrılabilir)
    // Bunu zaten AuthService'e koymayı planlamıştık ama servis olarak ayırmak daha temiz.
    Task UpdateStreakAsync(int userId);
}
