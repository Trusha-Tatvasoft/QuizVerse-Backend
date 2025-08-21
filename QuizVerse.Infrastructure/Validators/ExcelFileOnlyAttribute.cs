using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QuizVerse.Infrastructure.Validators;

public class ExcelFileOnlyAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            if (!Path.GetExtension(file.FileName).Equals(".xls", StringComparison.OrdinalIgnoreCase) &&
                !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(ErrorMessage ?? "Invalid file type. Please upload an Excel file.");
            }

            if (file.ContentType != "application/vnd.ms-excel" &&
                file.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return new ValidationResult(ErrorMessage ?? "Invalid file format. Only Excel files are supported.");
            }
        }

        return ValidationResult.Success;
    }
}
