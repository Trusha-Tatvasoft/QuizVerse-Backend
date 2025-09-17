using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class PlatformConfigurationRequestDTO
{
    [Required(ErrorMessage = "Qoute is Required")]
    public string Quote { get; set; } = null!;
    public IFormFile? Logo { get; set; } 
    public DefaultsColors DefaultsColors { get; set; } = null!;
}
