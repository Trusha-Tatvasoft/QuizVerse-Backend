using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class UserProfileSettingDto
{
    public string? FullName { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; } = string.Empty;
}
