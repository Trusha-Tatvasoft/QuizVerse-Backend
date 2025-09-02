using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class EmailTemplateActionRequestDTO
{
    [Required]
    public int Id { get; set; }

    [Required]
    public EmailTemplateActionType Action { get; set; }
}