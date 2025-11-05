using QuizVerse.Infrastructure.DTOs.RequestDTOs;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class GenerateQuizResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    // Optional: returned when successful
    public List<QuizQuestionDto>? Data { get; set; }
    public int? Count { get; set; }
    public List<QuestionSpecification>? Specifications { get; set; }

    // Optional: validation info
    public ValidationInfoDto? Validation { get; set; }

    // Optional: returned when AI or system error occurs
    public string? Error { get; set; }
    public string? RawResponse { get; set; }
}


public class ValidationInfoDto
{
    public bool ShouldProceed { get; set; }
    public string? Category { get; set; }
    public string? Reason { get; set; }
}


public class QuizQuestionDto
{
    public string? QueText { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int QueTypeId { get; set; }
    public string? QueTypeName { get; set; }
    public int QueDifficultyId { get; set; }
    public string? QueDifficultyName { get; set; }
    public List<QueOption>? QueOptionsAns { get; set; }
}

public class QueOption
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}