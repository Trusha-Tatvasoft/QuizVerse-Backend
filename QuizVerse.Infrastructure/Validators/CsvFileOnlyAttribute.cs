using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QuizVerse.Infrastructure.Validators;

public class CsvFileOnlyAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(ErrorMessage ?? "Invalid file type. Please upload a CSV file.");
            }

            if (file.ContentType != "text/csv" && file.ContentType != "application/vnd.ms-excel")
            {
                return new ValidationResult(ErrorMessage ?? "Invalid file format. Only CSV is supported.");
            }
        }

        return ValidationResult.Success;
    }
}
