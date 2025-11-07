using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuizReportRequestDto
{
    [Required]
    public int QuizId { get; set; }
    public int? ReportId { get; set; }
    [Required]
    [MinLength(40, ErrorMessage = "Reason must be at least 40 characters long.")]
    public string Reason { get; set; } = null!;
}