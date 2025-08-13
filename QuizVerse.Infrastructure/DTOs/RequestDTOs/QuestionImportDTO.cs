namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuestionImportDTO
{
    public string? Question { get; set; } 
    public string? Type { get; set; }
    public string? Difficulty { get; set; }
    public string? Category { get; set; }
    public string? Option1 { get; set; }
    public string? Option2 { get; set; }
    public string? Option3 { get; set; }
    public string? Option4 { get; set; }
    public string? CorrectAnswer { get; set; }

    public List<string> GetOptions()
    {
        var options = new List<string>();
        if (!string.IsNullOrWhiteSpace(Option1)) options.Add(Option1);
        if (!string.IsNullOrWhiteSpace(Option2)) options.Add(Option2);
        if (!string.IsNullOrWhiteSpace(Option3)) options.Add(Option3);
        if (!string.IsNullOrWhiteSpace(Option4)) options.Add(Option4);
        return options;
    }
}
