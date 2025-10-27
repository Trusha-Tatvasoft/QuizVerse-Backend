using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class EmailTemplatesRequestDTO
{
    public int Id { get; set; }
    [Required(ErrorMessage = "TemplateType is required")]
    public int TemplateType { get; set; }
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(255, ErrorMessage = "Title length can't be more than 255 characters.")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9 .,!?\-_@#]*$", ErrorMessage = "Email Title must start with letter/number.")]
    public string Title { get; set; } = null!;
    [Required(ErrorMessage = "Subject is required")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9 .,!?\-_@#]*$", ErrorMessage = "Email Subject must start with letter/number.")]
    public string Subject { get; set; } = null!;
    [Required(ErrorMessage = "Body is required")]
    [RegularExpression(@"^$|^\S[\s\S]*$", ErrorMessage = "Question text must not start with space.")]
    public string Body { get; set; } = null!;
    public bool? Status { get; set; }
}