using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

[Keyless]
public class SuccessResponseDTO
{
    [Column("success")]
    public bool Success { get; set; }
}
