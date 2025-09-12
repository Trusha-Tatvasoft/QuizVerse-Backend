namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class SubmitQuizRequestDTO
{
    public int QuizId { get; set; }

    public string QuizName { get; set; } = string.Empty;

    public int TimeTaken { get; set; }

    public LastVisitedQuestionAndAnswerDTO LastVisitedQuestionAndAnswers { get; set; } = new();
}

public class LastVisitedQuestionAndAnswerDTO
{
    public int QuestionId { get; set; }

    public string? GivenAnswer { get; set; }
}
