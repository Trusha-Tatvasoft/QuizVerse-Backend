using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuestionsListResponseDto
{
    [Column("id")]
    public int? Id { get; set; }
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }
    [JsonPropertyName("que_difficulty_id")]
    public int QueDifficultyId { get; set; }
    [JsonPropertyName("que_text")]
    public string QueText { get; set; } = null!;
    [JsonPropertyName("que_type_id")]
    public int QueTypeId { get; set; }

    [NotMapped]
    [JsonPropertyName("que_options_ans")]
    public List<QueOptionsAndAnswersDto> QueOptionsAns { get; set; } = new();
}