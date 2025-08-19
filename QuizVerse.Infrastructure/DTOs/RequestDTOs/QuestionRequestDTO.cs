using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuestionRequestDTO
{
    [Required(ErrorMessage = "QuestionTypeId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuestionTypeId must be greater than 0.")]
    public int QuestionTypeId { get; set; }

    [Required(ErrorMessage = "CategoryId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be greater than 0.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "DifficultyId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "DifficultyId must be greater than 0.")]
    public int DifficultyId { get; set; }

    [Required(ErrorMessage = "QuestionText is required.")]
    public string QuestionText { get; set; } = null!;

    public List<string>? Options { get; set; }

    [Required(ErrorMessage = "CorrectAnswer is required.")]
    public string CorrectAnswer { get; set; } = null!;
}
