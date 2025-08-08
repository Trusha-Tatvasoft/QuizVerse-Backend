namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuizCreationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int TotalTime { get; set; }
    public int DifficultyLevelId { get; set; }
    public int TotalQuestion { get; set; }
    public bool IsPaid { get; set; }
    public decimal? Price { get; set; }
    public int Status { get; set; } = 1; 
    public List<string> Tags { get; set; } = new();
}