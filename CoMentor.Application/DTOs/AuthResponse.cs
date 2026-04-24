namespace CoMentor.Application.DTOs
{
    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string Role { get; set; } = "Student";
        public UserDto? User { get; set; } // optional, kullanmak isterseniz
    }
}