using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizVerse.Infrastructure.DTOs;

public class TagsListDto
{
    [Column("id")]
    public int? Id { get; set; }

    [Column("name")]
    [Required(ErrorMessage = "Tag name is required.")]
    [MinLength(1, ErrorMessage = "Tag name must be at least 1 character long.")]
    [MaxLength(255, ErrorMessage = "Tag name cannot exceed 255 characters.")]
    [RegularExpression(@"^$|^\S[\s\S]*$", ErrorMessage = "Quiz tag must not start with space.")]
    public string Name { get; set; } = null!;
}