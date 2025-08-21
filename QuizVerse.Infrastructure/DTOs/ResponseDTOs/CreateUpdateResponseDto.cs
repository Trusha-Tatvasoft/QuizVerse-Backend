using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

[Keyless]
public class CreateUpdateResponseDto
{
    [Column("p_success")]
    public bool Success { get; set; }

    [Column("p_message")]
    public string Message { get; set; } = null!;
}