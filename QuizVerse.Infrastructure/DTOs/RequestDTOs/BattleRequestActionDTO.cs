using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class BattleRequestActionDTO
{
    [Required(ErrorMessage = "RequestId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "RequestId must be greater than zero.")]
    public int RequestId { get; set; }
    
    [Required(ErrorMessage = "Status is required.")]
    [EnumDataType(typeof(BattleRequestStatus), ErrorMessage = "Invalid status value. Allowed values are: 1 (Accepted), 2 (Rejected).")]
    public int Status { get; set; }   // 1 = Accept, 2 = Reject
}
