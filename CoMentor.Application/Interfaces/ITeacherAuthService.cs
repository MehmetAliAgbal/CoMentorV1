using CoMentor.Application.DTOs;

namespace CoMentor.Application.Interfaces;

public interface ITeacherAuthService
{
    Task<TeacherAuthResponse?> RegisterAsync(TeacherRegisterRequest request);
    Task<TeacherAuthResponse?> LoginAsync(TeacherLoginRequest request);
}
