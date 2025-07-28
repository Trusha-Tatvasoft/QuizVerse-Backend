using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs;

public class UpdateUserDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name can't be longer than 100 characters.")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(100, ErrorMessage = "Email can't be longer than 100 characters.")]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a properly formatted email address.")]
    public string Email { get; set; } = null!;

    [StringLength(50, ErrorMessage = "Username can't be longer than 50 characters.")]
    public string UserName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
}
