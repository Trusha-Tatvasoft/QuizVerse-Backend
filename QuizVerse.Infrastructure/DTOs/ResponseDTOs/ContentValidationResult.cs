namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class ContentValidationResult
{
    public bool IsValid { get; set; }
    public bool IsMatch { get; set; }
    public string Category { get; set; } = "";
    public string RequestedCategory { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool ValidationFailed { get; set; }

    public bool ShouldProceed => IsValid && IsMatch;
}