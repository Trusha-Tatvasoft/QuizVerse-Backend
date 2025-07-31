using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(255, ErrorMessage = "Full name must not exceed 255 characters.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Username is required.")]
        [RegularExpression(@"^[a-zA-Z0-9!@#\$%\^&\*\(\)_\+\-=\[\]\{\};:,.<>\/?\\|`~]+$",
            ErrorMessage = "Username can only contain letters, numbers, and special characters.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email must not exceed 255 characters.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(30, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 30 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
             ErrorMessage = "Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Bio must not exceed 500 characters.")]
        public string? Bio { get; set; }
    }
}
