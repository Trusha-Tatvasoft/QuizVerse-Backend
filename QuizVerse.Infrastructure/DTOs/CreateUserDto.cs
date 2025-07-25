using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name can't be longer than 100 characters.")]
        public string FullName { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Username can't be longer than 50 characters.")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(100, ErrorMessage = "Email can't be longer than 100 characters.")]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a properly formatted email address.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(30, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 30 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character.")]
        public string Password { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Bio can't be longer than 500 characters.")]
        public string? Bio { get; set; }

        [StringLength(250, ErrorMessage = "Profile picture URL can't be longer than 250 characters.")]
        public string? ProfilePic { get; set; }
    }
}
