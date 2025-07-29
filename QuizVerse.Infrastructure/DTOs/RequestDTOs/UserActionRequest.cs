using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class UserActionRequest
{
    [Required]
    public int Id { get; set; }

    [Required]
    public UserActionType Action { get; set; }

    public UserStatus? NewStatus { get; set; }
}
