using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Validators;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class UpdateProfilePicRequestDto
{
    [AllowedImage(ErrorMessage = "Invalid profile picture. Only .jpg, .jpeg, .png, and .gif files are allowed, with a maximum size of 5MB.")]
    public IFormFile? ProfilePic { get; set; }
}
