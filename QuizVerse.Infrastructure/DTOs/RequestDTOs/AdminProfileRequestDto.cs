using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Validators;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class AdminProfileRequestDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name can't be longer than 100 characters.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Username is required.")]
    [RegularExpression(@"^[a-zA-Z0-9!@#\$%\^&\*\(\)_\+\-=\[\]\{\};:,.<>\/?\\|`~]+$",
            ErrorMessage = "Username can only contain letters, numbers, and special characters.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(100, ErrorMessage = "Email can't be longer than 100 characters.")]
    public string Email { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Bio can't be longer than 500 characters.")]
    public string? Bio { get; set; }

}
