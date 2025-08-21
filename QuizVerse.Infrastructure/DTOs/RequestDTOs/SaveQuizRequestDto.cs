using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class SaveQuizRequestDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Quiz title is required.")]
    [MinLength(1, ErrorMessage = "Quiz name must be at least 1 character long.")]
    [MaxLength(255, ErrorMessage = "Quiz name cannot exceed 255 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Quiz description is required.")]
    [MinLength(1, ErrorMessage = "Quiz description must be at least 1 character long.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quiz total time is required.")]
    [Range(2, 180, ErrorMessage = "Total time must be between 2 and 180 minutes.")]
    public int TotalTime { get; set; }

    [Required(ErrorMessage = "Difficulty level is required.")]
    public int DifficultyLevelId { get; set; }

    [Required(ErrorMessage = "Total questions number is required.")]
    [Range(5, 200, ErrorMessage = "Total questions must be between 5 and 200.")]
    public int TotalQuestion { get; set; }

    public bool IsPaid { get; set; }

    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Price must be a positive value.")]
    public decimal? Price { get; set; }
    public int Status { get; set; } = (int)QuizStatus.Draft;
    public List<TagsListDto>? Tags { get; set; } = [];
    public List<QuestionsListRequestDto>? Questions { get; set; } = [];
    public List<NoOfQuestionPerDifficultyDto>? NoOfQuestionsPerDifficulty { get; set; } = [];
}