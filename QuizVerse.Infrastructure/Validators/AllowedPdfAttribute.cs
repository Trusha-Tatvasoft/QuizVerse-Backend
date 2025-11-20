using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace QuizVerse.Infrastructure.Validators
{
    public class AllowedPdfAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile file)
                return ValidationResult.Success;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return new ValidationResult(Constants.INVALID_PDF_FILE_TYPE_MESSAGE);
            }

            if (file.ContentType != "application/pdf" && file.ContentType != "application/x-pdf")
            {
                return new ValidationResult(Constants.INVALID_PDF_FILE_TYPE_MESSAGE);
            }

            return ValidationResult.Success;
        }
    }
}
