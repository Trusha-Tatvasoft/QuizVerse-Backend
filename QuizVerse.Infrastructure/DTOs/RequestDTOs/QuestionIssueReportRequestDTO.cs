using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuestionIssueReportRequestDTO
{
    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(255, ErrorMessage = "Description must not exceed 255 characters.")]
    [MinLength(40, ErrorMessage = "Description must be at least 40 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "QuizId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuizId must be greater than 0.")]
    public int QuizId { get; set; }

    [Required(ErrorMessage = "QuestionId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuestionId must be greater than 0.")]
    public int QuestionId { get; set; }
    public int? ReportId { get; set; }
}
