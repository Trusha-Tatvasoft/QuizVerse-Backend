using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Validators;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class CsvUploadRequestDTO
{
    [Required(ErrorMessage = "CSV file is required.")]
    [CsvFileOnly(ErrorMessage = "Only CSV files are allowed.")]
    public IFormFile File { get; set; } = null!;
}
