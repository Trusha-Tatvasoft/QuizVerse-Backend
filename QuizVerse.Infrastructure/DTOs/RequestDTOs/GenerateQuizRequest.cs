using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;


public class GenerateQuestionRequestCommonFields
{
    public List<QuestionGenerationFormatDto> QuestionSpec { get; set; } = [];
    public int CategoryId { get; set; }
    public string? Category { get; set; }
}

public class GenerateQuizRequest : GenerateQuestionRequestCommonFields
{
    [Required]
    [MaxLength(500, ErrorMessage = "Prompt text cannot exceed 500 characters.")]
    [MinLength(25, ErrorMessage = "Prompt text must be at least 25 characters long.")]
    public string? Prompt { get; set; }
}

public class QuestionGenerationFormatDto
{
    public int QuestionDifficultyId { get; set; }
    public string QuestionDifficultyName { get; set; } = string.Empty;
    public List<QuestionPerQuestionType> QuestionPerQuestionType { get; set; } = [];
}

public class QuestionPerQuestionType
{
    public int QuestionPerQuestionTypeId { get; set; }
    public string QuestionPerQuestionTypeName { get; set; } = string.Empty;
    public int NoOfQuesitons { get; set; }
}

public class QuestionSpecification
{
    public string Type { get; set; } = string.Empty; // mcq, fill, truefalse, short 
    public string Difficulty { get; set; } = string.Empty; // easy, medium, hard 
    public int Count { get; set; }
}