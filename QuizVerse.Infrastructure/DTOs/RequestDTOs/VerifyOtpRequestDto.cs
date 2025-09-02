using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class VerifyOtpRequestDto
{
    [Required]
    public string Otp { get; set; } = string.Empty;
}
