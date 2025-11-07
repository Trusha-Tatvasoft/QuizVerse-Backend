using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuizAndQuestionReportAction
{
    [Required]
    public int ReportId { get; set; }
    [Required]
    public int QuestionOrQuizIssueReportNewStatus { get; set; }
}