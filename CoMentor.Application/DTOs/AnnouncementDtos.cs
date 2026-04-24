using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs;

public class AnnouncementDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    public int? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string TargetAudience { get; set; } = "Students";
}

public class CreateAnnouncementRequest
{
    [Required]
    public string Title { get; set; } = null!;
    
    [Required]
    public string Message { get; set; } = null!;
    
    public int? ClassroomId { get; set; }
    public int? UserId { get; set; }
    public int? ParentId { get; set; }
    public string TargetAudience { get; set; } = "Students";
}
