using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionsListResponseDto
{
    [Column("id")]
    public int? Id { get; set; }

    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = null!;

    [JsonPropertyName("queDifficultyId")]
    public int QueDifficultyId { get; set; }

    [JsonPropertyName("queDifficultyName")]
    public string QueDifficultyName { get; set; } = null!;

    [JsonPropertyName("queText")]
    public string QueText { get; set; } = null!;

    [JsonPropertyName("queTypeId")]
    public int QueTypeId { get; set; }

    [JsonPropertyName("queTypeName")]
    public string QueTypeName { get; set; } = null!;
 
    [NotMapped]
    [JsonPropertyName("queOptionsAns")]
    public List<QueOptionsAndAnswersDto> QueOptionsAns { get; set; } = [];
}
