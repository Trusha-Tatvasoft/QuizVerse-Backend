using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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
    [Column("que_options_ans")]
    public string? QueOptionsAnsJson { get; set; }

    [NotMapped]
    public List<QueOptionsAndAnsDto> QueOptionsAns =>
     string.IsNullOrWhiteSpace(QueOptionsAnsJson)
         ? new()
         : JsonSerializer.Deserialize<List<QueOptionsAndAnsDto>>(
             QueOptionsAnsJson,
             new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
           )!;
}
