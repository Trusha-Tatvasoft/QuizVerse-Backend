using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

[Keyless]
public class RawStartQuizDto
{
    [Column("out_quiz_id")]
    public int QuizId { get; set; }

    [Column("quiz_name")]
    public string QuizName { get; set; } = string.Empty;

    [Column("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    [Column("total_time")]
    public decimal TotalTime { get; set; }

    [Column("total_question")]
    public int TotalQuestion { get; set; }

    [Column("quiz_question_id")]
    public int QuizQuestionId { get; set; }

    [Column("question_name")]
    public string QuestionName { get; set; } = string.Empty;

    [Column("question_type")]
    public string QuestionType { get; set; } = string.Empty;

    [Column("options")]
    public string Options { get; set; } = "[]"; // raw JSON string
}

public class QuizStartResponseDto
{
    public int QuizId { get; set; }
    public string QuizName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalTime { get; set; }
    public int TotalQuestion { get; set; }
    public int QuizQuestionId { get; set; }
    public string QuestionName { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public List<OptionResponseDto> Options { get; set; } = new();
}

public class OptionResponseDto
{
    [JsonPropertyName("optionId")]
    public int OptionId { get; set; }
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
