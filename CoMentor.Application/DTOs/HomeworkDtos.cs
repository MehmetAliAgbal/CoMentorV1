using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs;

public class HomeworkDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    
    public string Subject { get; set; } = null!;
    public string Topic { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    
    // Öğrenci paneli için ödev durumu
    public bool IsCompleted { get; set; }
}

public class HomeworkStudentStatusDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateHomeworkRequest
{
    [Required]
    public string Subject { get; set; } = null!;
    
    [Required]
    public string Topic { get; set; } = null!;
    
    public string? Description { get; set; }
    
    [Required]
    public DateTime DueDate { get; set; }
    
    public int? ClassroomId { get; set; }
    public int? UserId { get; set; }
}
