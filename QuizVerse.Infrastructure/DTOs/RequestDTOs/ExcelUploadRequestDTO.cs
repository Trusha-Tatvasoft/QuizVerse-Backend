using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Validators;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class ExcelUploadRequestDTO
{
    [Required(ErrorMessage = "Excel file is required.")]
    [ExcelFileOnlyAttribute(ErrorMessage = "Only Excel files are allowed.")]

    public IFormFile File { get; set; } = null!;
}
