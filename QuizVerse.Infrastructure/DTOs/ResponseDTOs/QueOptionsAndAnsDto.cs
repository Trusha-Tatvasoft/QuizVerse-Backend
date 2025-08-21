using System.ComponentModel.DataAnnotations.Schema;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QueOptionsAndAnsDto
{
    [Column("id")]
    public int Id { get; set; }
    [Column("questionId")]
    public int QuestionId { get; set; }
    [Column("key")]
    public string Key { get; set; } = null!;
    [Column("value")]
    public string Value { get; set; } = null!;
}