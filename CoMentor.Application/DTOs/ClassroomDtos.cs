using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs;

public class ClassroomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int StudentCount { get; set; }
}

public class CreateClassroomRequest
{
    [Required]
    public string Name { get; set; } = null!;
    
    public string? Description { get; set; }
}

public class UpdateClassroomRequest
{
    [Required]
    public string Name { get; set; } = null!;
    
    public string? Description { get; set; }
}
