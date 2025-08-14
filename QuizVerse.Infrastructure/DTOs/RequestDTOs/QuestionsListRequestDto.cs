using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuestionsListRequestDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Quesiton difficulty is required.")]
    public int QueDifficultyId { get; set; }

    [Required(ErrorMessage = "Question text is required.")]
    [MinLength(1, ErrorMessage = "Question text must be at least 1 character long.")]
    public string QueText { get; set; } = null!;

    [Required(ErrorMessage = "Question type is required.")]
    public int QueTypeId { get; set; }
    public List<QueOptionsAndAnswersDto>? QueOptionsAns { get; set; } = new();
}