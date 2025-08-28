using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class BattleResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int BattleType { get; set; } = (int)Enums.BattleType.Permanent;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int TotalTime { get; set; }

    public int TotalQuestion { get; set; }

    public int TotalXp { get; set; }

    public int Status { get; set; } = (int)BattleCreationStatus.Active;

    public int DifficultyLevelId { get; set; }

    public int CategoryId { get; set; }

    // JSON Questions
    public string? QuestionsJson { get; set; }

    [NotMapped]
    public List<QuestionsListResponseDto> Questions =>
        string.IsNullOrWhiteSpace(QuestionsJson)
            ? new()
            : JsonSerializer.Deserialize<List<QuestionsListResponseDto>>(
                QuestionsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
              )!;

    // JSON Difficulty mapping
    public string? QuestionsDifficultyJson { get; set; }

    [NotMapped]
    public List<BattleQuestionDifficultyDTO> QuestionsDifficulty =>
        string.IsNullOrWhiteSpace(QuestionsDifficultyJson)
            ? new()
            : JsonSerializer.Deserialize<List<BattleQuestionDifficultyDTO>>(
                QuestionsDifficultyJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
              )!;
}
