using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class SubmitQuizRequestDTO
{
    [Required(ErrorMessage = "QuizId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuizId must be greater than 0.")]
    public int QuizId { get; set; }

    [Required(ErrorMessage = "QuizName is required.")]
    public string QuizName { get; set; } = string.Empty;

    [Required(ErrorMessage = "TimeTaken is required.")]
    public int TimeTaken { get; set; }

    public LastVisitedQuestionAndAnswerDTO LastVisitedQuestionAndAnswers { get; set; } = new();
}

public class LastVisitedQuestionAndAnswerDTO
{
    public int QuestionId { get; set; }

    public string? GivenAnswer { get; set; }
}
