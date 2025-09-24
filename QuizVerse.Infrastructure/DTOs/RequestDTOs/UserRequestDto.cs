using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Validators;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;
public class UserRequestDto
{
    public int? Id { get; set; }

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

    [StringLength(30, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 30 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character.")]
    public string? Password { get; set; }

    [StringLength(500, ErrorMessage = "Bio can't be longer than 500 characters.")]
    public string? Bio { get; set; }

    [AllowedImage(ErrorMessage = "Invalid profile picture. Only .jpg, .jpeg, .png, and .gif files are allowed, with a maximum size of 10MB.")]
    public IFormFile? ProfilePic { get; set; }

    public bool IsRegister { get; set; } = false;
}


