using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionPoolListDto
{
    public int Id { get; set; }

    [Column("categoryId")]
    public int CategoryId { get; set; }
    [Column("categoryName")]
    public string CategoryName { get; set; } = null!;
    [Column("queDifficultyId")]
    public int QueDifficultyId { get; set; }
    [Column("queDifficultyName")]
    public string QueDifficultyName { get; set; } = null!;
    [Column("queText")]
    public string QueText { get; set; } = null!;
    [Column("queTypeId")]
    public int QueTypeId { get; set; }
    [Column("queTypeName")]
    public string QueTypeName { get; set; } = null!;
    [Column("queOptionsAns")]
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
