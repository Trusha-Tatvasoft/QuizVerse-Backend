namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizOverviewResponseDto
{
    public int QuizId { get; set; }
    public string QuizName { get; set; } = null!;
    public decimal TotalTime { get; set; }
    public int TotalQuestion { get; set; }
    public string QuizDifficultyName { get; set; } = null!;
    public string QuizCategoryName { get; set; } =  null!;
    public bool IsPaid { get; set; }
    public decimal QuizPrice { get; set; }
    public string Description { get; set; } = null!;
}
