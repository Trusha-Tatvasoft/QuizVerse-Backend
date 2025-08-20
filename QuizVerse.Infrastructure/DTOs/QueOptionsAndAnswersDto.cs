using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuizVerse.Infrastructure.DTOs;

public class QueOptionsAndAnswersDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
 
    [JsonPropertyName("questionId")]
    public int? QuestionId { get; set; }
 
    [JsonPropertyName("key")]
    [Required(ErrorMessage = "Option Name is required.")]
    public string Key { get; set; } = null!;
 
    [JsonPropertyName("value")]
    [Required(ErrorMessage = "Option Value is required.")]
    public string Value { get; set; } = null!;
}
