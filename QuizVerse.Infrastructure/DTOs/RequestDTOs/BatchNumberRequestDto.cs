using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class BatchNumberRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "BatchNumber must be greater than 0")]
    public int BatchNumber { get; set; }
}