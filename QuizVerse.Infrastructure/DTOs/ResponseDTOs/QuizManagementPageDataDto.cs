namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizManagementPageDataDto
{
    public long TotalQuiz { get; set; }
    public long TotalParticipants { get; set; }
    public long ActiveQuiz { get; set; }
    public long TotalQuestions { get; set; }
}
