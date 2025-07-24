using QuizVerse.Domain.Entities;

namespace QuizVerse.Infrastructure.DTOs
{
    public class LoginResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}