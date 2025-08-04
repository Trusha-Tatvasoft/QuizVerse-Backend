using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common;
using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.Validators;

public class AllowedImageAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file) return ValidationResult.Success;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!SystemConstants.IMAGE_ALLOWED_EXTENSIONS.Contains(extension))
        {
            return new ValidationResult(Constants.INVALID_IMAGE_FILE_TYPE_MESSAGE);
        }

        if (file.Length > SystemConstants.IMAGE_UPLOAD_MAX_SIZE)
        {
            return new ValidationResult(Constants.IMAGE_FILE_SIZE_EXCEEDED_MESSAGE);
        }

        return ValidationResult.Success;
    }
}
