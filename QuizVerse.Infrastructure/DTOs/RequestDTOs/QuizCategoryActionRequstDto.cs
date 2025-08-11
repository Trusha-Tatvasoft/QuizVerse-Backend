using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuizCategoryActionRequestDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    public QuizCategoryActionType Action { get; set; }
    public QuizCategoryStatus? NewStatus { get; set; }
}
