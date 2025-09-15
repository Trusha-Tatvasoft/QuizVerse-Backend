using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizQuestionResponseDto
{
    public int QuizQuestionId { get; set; }
    public string QuestionName { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public List<OptionResponseDto> Options { get; set; } = new();
}

[Keyless]
public class RawQuizQuestionDto
{
    [Column("quiz_question_id")]
    public int QuizQuestionId { get; set; }
    [Column("question_name")]
    public string QuestionName { get; set; } = null!;
    [Column("question_type")]
    public string QuestionType { get; set; } = null!;

    [Column("options")]
    public string Options { get; set; } = "[]"; // raw JSON string
}
