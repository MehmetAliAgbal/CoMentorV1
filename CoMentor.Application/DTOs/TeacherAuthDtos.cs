using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs;

public class TeacherRegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
    
    [Required, MinLength(6)]
    public string Password { get; set; } = null!;
    
    [Required]
    public string Name { get; set; } = null!;
    
    [Required]
    public string Surname { get; set; } = null!;
    
    public string? Branch { get; set; }
    public string? AvatarUrl { get; set; }
}

public class TeacherLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;
    
    [Required]
    public string Password { get; set; } = null!;
}

public class TeacherDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string? Branch { get; set; }
    public string? AvatarUrl { get; set; }
}

public class TeacherAuthResponse
{
    public int TeacherId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public TeacherDto Teacher { get; set; } = null!;
}
