using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuestionDifficultyRequestDTO
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Question Difficulty Name is required.")]
    [MaxLength(255, ErrorMessage = "Name length can't be more than 256 characters.")]
    [RegularExpression(@"^[A-Za-z][A-Za-z ]*$", ErrorMessage = "Difficulty Name must contain only alphabets")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Question Difficulty Description is required.")]
    [MaxLength(500, ErrorMessage = "Description length can't be more than 256 characters.")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9 .,!?\-_@#]*$", ErrorMessage = "Difficulty Description must start with letter/number.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xp Gained Per Question is required.")]
    public int XpGainedPerQuestion { get; set; }
}
