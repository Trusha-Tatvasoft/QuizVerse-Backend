using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuizVerse.Infrastructure.DTOs;

public class NoOfQuestionPerDifficultyDto
{
    [Required(ErrorMessage = "Question Difficulty level is required.")]
    [JsonPropertyName("queDifficultyName")]
    public string QueDifficultyName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Number of questions is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Number of questions must be a non-negative integer.")]
    [JsonPropertyName("noOfQuestions")]
    public int NoOfQuestions { get; set; } = 0;
}
