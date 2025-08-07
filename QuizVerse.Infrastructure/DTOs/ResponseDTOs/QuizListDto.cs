namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizListDto
{
    public int Id { get; set; }

    public string QuizTitle { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string QuizDifficultyLevel { get; set; } = null!;

    public int TotalQuestion { get; set; }

    public int NoOfPersonAttempted { get; set; }

    public int Status { get; set; }

    public DateTime CreatedDate { get; set; }
}
