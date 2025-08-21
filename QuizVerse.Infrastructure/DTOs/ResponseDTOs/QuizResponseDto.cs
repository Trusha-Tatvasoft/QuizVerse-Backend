using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class QuizResponseDto
{
    [Column("id")]
    public int? Id { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("quizCategoryId")]
    public int CategoryId { get; set; }
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    [Column("totalTime")]
    public int TotalTime { get; set; }
    [Column("difficultyLevelId")]
    public int DifficultyLevelId { get; set; }
    [Column("totalQuestion")]
    public int TotalQuestion { get; set; }
    [Column("isPaid")]
    public bool IsPaid { get; set; }
    [Column("price")]
    public decimal? Price { get; set; }
    [Column("status")]
    public int Status { get; set; } = (int)QuizStatus.Draft;


    [Column("tags")]
    public string? TagsJson { get; set; }
    [NotMapped]
    public List<TagsListDto>? Tags
    =>
     string.IsNullOrWhiteSpace(TagsJson)
         ? new()
         : JsonSerializer.Deserialize<List<TagsListDto>>(
             TagsJson,
             new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
           )!;


    [Column("questions")]
    public string? QuestionsJson { get; set; }
    [NotMapped]
    public List<QuestionsListResponseDto> Questions =>
     string.IsNullOrWhiteSpace(QuestionsJson)
         ? new()
         : JsonSerializer.Deserialize<List<QuestionsListResponseDto>>(
             QuestionsJson,
             new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
           )!;

    [Column("noOfQuestionsPerDifficulty")]
    public string? NoOfQuestionsPerDifficultyJson { get; set; }
    [NotMapped]
    public List<NoOfQuestionPerDifficultyDto> NoOfQuestionsPerDifficulty =>
     string.IsNullOrWhiteSpace(NoOfQuestionsPerDifficultyJson)
         ? new()
         : JsonSerializer.Deserialize<List<NoOfQuestionPerDifficultyDto>>(
             NoOfQuestionsPerDifficultyJson,
             new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
           )!;
}