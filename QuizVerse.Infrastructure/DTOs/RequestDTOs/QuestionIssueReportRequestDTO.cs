using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuestionIssueReportRequestDTO
{
    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "QuizId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuizId must be greater than 0.")]
    public int QuizId { get; set; }

    [Required(ErrorMessage = "QuestionId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuestionId must be greater than 0.")]
    public int QuestionId { get; set; }
}
