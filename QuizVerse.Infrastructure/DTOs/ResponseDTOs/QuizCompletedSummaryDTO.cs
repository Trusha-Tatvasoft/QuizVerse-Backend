namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizCompletedSummaryDTO
{
    public string QuizName { get; set; } = null!;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public double ScorePercentage { get; set; }
    public string Grade { get; set; } = null!;
    public TimeSpan TimeSpent { get; set; }
    public int XpEarned { get; set; }
}
