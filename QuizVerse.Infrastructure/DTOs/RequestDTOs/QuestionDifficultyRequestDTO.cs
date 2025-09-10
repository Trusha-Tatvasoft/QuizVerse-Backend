using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;
public class QuestionDifficultyRequestDTO
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Question Difficulty Name is required.")]
    [MaxLength(256, ErrorMessage = "Name length can't be more than 256 characters.")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xp Gained Per Question is required.")]
    public int XpGainedPerQuestion { get; set; }
}
