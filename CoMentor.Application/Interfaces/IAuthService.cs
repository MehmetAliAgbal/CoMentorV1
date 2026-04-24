using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces
{
    public interface IAuthService
    {
        // Öğrenci
        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        
        // Veli
        Task<AuthResponse?> ParentRegisterAsync(ParentRegisterRequest request);
        Task<AuthResponse?> ParentLoginAsync(LoginRequest request);
    }
}