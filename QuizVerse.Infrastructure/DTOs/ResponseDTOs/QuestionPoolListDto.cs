using System.ComponentModel.DataAnnotations.Schema;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionPoolListDto
{
    public int Id { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }
    [Column("category_name")]
    public string CategoryName { get; set; } = null!;
    [Column("que_difficulty_id")]
    public int QueDifficultyId { get; set; }
    [Column("que_difficulty_name")]
    public string QueDifficultyName { get; set; } = null!;
    [Column("que_text")]
    public string QueText { get; set; } = null!;
    [Column("que_type_id")]
    public int QueTypeId { get; set; }
    [Column("que_type_name")]
    public string QueTypeName { get; set; } = null!;
    [NotMapped]
    public List<QueOptionsAndAnsDto> QueOptionsAns { get; set; } = new();
}
