using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs
{
    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "Reset Password Token is required.")]
        public string? ResetPasswordToken { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character. It must be at least 8 characters long.")]
        public string? Password { get; set; }
    }

    public class EmailForResetPasswordDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; }
    }

    public class ResetPasswordTokenDTO
    {
        [Required(ErrorMessage = "Reset Password Token is required.")]
        public string? ResetPasswordToken { get; set; }
    }
}