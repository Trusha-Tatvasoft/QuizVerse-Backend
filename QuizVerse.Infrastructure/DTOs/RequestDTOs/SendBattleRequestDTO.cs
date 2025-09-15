using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class SendBattleRequestDTO
{
    [Required(ErrorMessage = "Receiver username is required.")]
    public string ReceiverUsername { get; set; } = null!;

    [Required(ErrorMessage = "Battle ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Battle ID must be greater than 0.")]
    public int BattleId { get; set; }
}
