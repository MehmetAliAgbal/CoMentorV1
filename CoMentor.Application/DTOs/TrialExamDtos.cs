using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs
{
    /// <summary>
    /// Ders bazında net puanı için DTO
    /// </summary>
    public class SubjectScoreRequest
    {
        [Required]
        public int SubjectId { get; set; }

        [Range(0, 100)]
        public int CorrectAnswers { get; set; } = 0;

        [Range(0, 100)]
        public int WrongAnswers { get; set; } = 0;

        [Range(0, 100)]
        public int EmptyAnswers { get; set; } = 0;
    }

    /// <summary>
    /// Yeni deneme oluşturma isteği
    /// </summary>
    public class CreateTrialExamRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [Required]
        [RegularExpression("^(TYT|AYT)$", ErrorMessage = "Deneme türü TYT veya AYT olmalıdır")]
        public string ExamType { get; set; } = null!;

        [Required]
        public DateTime ExamDate { get; set; }

        public int? DurationMinutes { get; set; }

        public string? Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<SubjectScoreRequest> SubjectScores { get; set; } = new();
    }

    /// <summary>
    /// Deneme güncelleme isteği
    /// </summary>
    public class UpdateTrialExamRequest
    {
        [StringLength(100, MinimumLength = 1)]
        public string? Name { get; set; }

        public DateTime? ExamDate { get; set; }

        public int? DurationMinutes { get; set; }

        public string? Notes { get; set; }

        public List<SubjectScoreRequest>? SubjectScores { get; set; }
    }

    /// <summary>
    /// Ders bazında net sonucu DTO
    /// </summary>
    public class SubjectScoreDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = null!;
        public string SubjectShortName { get; set; } = null!;
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int EmptyAnswers { get; set; }
        public double NetScore { get; set; }
    }

    /// <summary>
    /// Tek bir deneme için detaylı DTO
    /// </summary>
    public class TrialExamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ExamType { get; set; } = null!;
        public DateTime ExamDate { get; set; }
        public double TotalNet { get; set; }
        public int? Ranking { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SubjectScoreDto> SubjectScores { get; set; } = new();
    }

    /// <summary>
    /// Liste görünümü için özet DTO
    /// </summary>
    public class TrialExamListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ExamType { get; set; } = null!;
        public DateTime ExamDate { get; set; }
        public double TotalNet { get; set; }
        public double? NetChange { get; set; } // Bir önceki denemeye göre değişim
    }

    /// <summary>
    /// Deneme listesi ve istatistikler için ana DTO
    /// </summary>
    public class TrialExamListResponse
    {
        public List<TrialExamListItemDto> Trials { get; set; } = new();
        public TrialExamStatsDto Stats { get; set; } = new();
    }

    /// <summary>
    /// Deneme istatistikleri DTO
    /// </summary>
    public class TrialExamStatsDto
    {
        public int TotalTrials { get; set; }
        public double AverageNet { get; set; } // Ortalama
        public double HighestNet { get; set; } // En Yüksek
        public double? LastChange { get; set; } // Son Değişim
        public DateTime? LastTrialDate { get; set; }
    }
}

