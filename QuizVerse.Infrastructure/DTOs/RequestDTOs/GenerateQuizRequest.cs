namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class GenerateQuizRequest
{
    public string? Prompt { get; set; }
    public List<QuestionGenerationFormatDto> QuestionSpec { get; set; } = [];
    public string? Category { get; set; }
}


public class QuestionGenerationFormatDto
{
    public string QuestionDifficultyName { get; set; } = string.Empty;
    public List<QuestionPerQuestionType> QuestionPerQuestionType { get; set; } = [];
}

public class QuestionPerQuestionType
{
    public string QuestionPerQuestionTypeName { get; set; } = string.Empty;
    public int NoOfQuesitons { get; set; }
}

public class QuestionSpecification
{
    public string Type { get; set; } = string.Empty; // mcq, fill, truefalse, short 
    public string Difficulty { get; set; } = string.Empty; // easy, medium, hard 
    public int Count { get; set; }
}