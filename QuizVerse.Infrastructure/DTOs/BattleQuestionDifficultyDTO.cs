using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class BattleQuestionDifficultyDTO
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Question difficulty is required.")]
    public int QueDifficultyId { get; set; }

    [Required(ErrorMessage = "Number of questions is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Number of questions must be a non-negative integer.")]
    [JsonPropertyName("noOfQues")]
    public int NoOfQues { get; set; }

    [Required(ErrorMessage = "Time per question is required.")]
    [Range(5, 3600, ErrorMessage = "Time per question must be between 5 and 3600 seconds.")]
    [JsonPropertyName("timePerQuestion")]
    public int TimePerQuestion { get; set; } 
}