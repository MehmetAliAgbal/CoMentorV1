using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs
{
    /// <summary>
    /// Yeni ders ekleme isteği
    /// </summary>
    public class CreateSubjectRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string ShortName { get; set; } = null!;

        public string? ColorHex { get; set; }

        [RegularExpression("^(TYT|AYT|BOTH)$", ErrorMessage = "ExamType TYT, AYT veya BOTH olmalıdır")]
        public string? ExamType { get; set; }

        [Range(1, 100)]
        public int? MaxQuestions { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Ders bilgisi DTO
    /// </summary>
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? ColorHex { get; set; }
        public string? ExamType { get; set; }
        public int? MaxQuestions { get; set; }
        public bool IsActive { get; set; }
    }
}

