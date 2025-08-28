using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class SaveBattleRequestDTO : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Battle title is required.")]
    [MinLength(1, ErrorMessage = "Battle title must be at least 1 character long.")]
    [MaxLength(255, ErrorMessage = "Battle title cannot exceed 255 characters.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Battle description is required.")]
    [MinLength(1, ErrorMessage = "Battle description must be at least 1 character long.")]
    public string Description { get; set; } = null!;

    [Required(ErrorMessage = "Difficulty level is required.")]
    public int DifficultyLevelId { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public int Status { get; set; } = (int)BattleCreationStatus.Active;

    [Required(ErrorMessage = "BattleType is required.")]
    public int BattleType { get; set; } = (int)Enums.BattleType.Permanent;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required(ErrorMessage = "TotalTime is required.")]
    [Range(2, 180, ErrorMessage = "Total time must be between 2 and 180 minutes.")]
    public decimal TotalTime { get; set; }

    [Required(ErrorMessage = "TotalQuestion is required.")]
    [Range(5, 100, ErrorMessage = "Total questions must be between 5 and 100.")]
    public int TotalQuestion { get; set; }

    [Required(ErrorMessage = "TotalXp is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "TotalXp must be greater than zero.")]
    public int TotalXp { get; set; }

    public List<QuestionsListRequestDto>? Questions { get; set; } = [];

    public List<BattleQuestionDifficultyDTO>? QuestionsDifficulty { get; set; } = [];

    public int QuizTypes { get; set; } = (int)QuizType.Battle;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BattleType == (int)Enums.BattleType.TimeLimited)
        {
            if (!StartDate.HasValue)
            {
                yield return new ValidationResult("StartDate is required for time-limited battles.", [nameof(StartDate)]);
            }

            if (!EndDate.HasValue)
            {
                yield return new ValidationResult("EndDate is required for time-limited battles.", [nameof(EndDate)]);
            }

            if (StartDate.HasValue && EndDate.HasValue && StartDate.Value >= EndDate.Value)
            {
                yield return new ValidationResult("StartDate must be earlier than EndDate.", [nameof(StartDate), nameof(EndDate)]);
            }
        }
    }
}