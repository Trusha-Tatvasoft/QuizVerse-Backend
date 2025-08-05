namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizCategoryDTO
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Icon { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public int QuizCount { get; set; }
}
