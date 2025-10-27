using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuizActionDataDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    public UserActionType Action { get; set; }

    public QuizStatus? NewStatus { get; set; }
}
