using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public class AllowedImageAttribute : ValidationAttribute
{
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".gif"];
    private readonly long _maxFileSize = 10 * 1024 * 1024; 

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file) return ValidationResult.Success;


        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_allowedExtensions.Contains(extension))
        {
            return new ValidationResult($"Only image files are allowed: {string.Join(", ", _allowedExtensions)}");
        }

        if (file.Length > _maxFileSize)
        {
            return new ValidationResult("Maximum allowed file size is 10MB.");
        }

        return ValidationResult.Success;
    }
}