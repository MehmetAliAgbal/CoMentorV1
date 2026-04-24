using System.ComponentModel.DataAnnotations;

namespace CoMentor.Application.DTOs
{
    public class ParentRegisterRequest
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Surname { get; set; } = null!;
        
        [Required]
        public int StudentId { get; set; } // Hangi öğrencinin velisi
    }
}
