using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs;

public class QuizRatingDTO
{
    [Required(ErrorMessage = "QuizId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuizId must be greater than 0.")]
    public int QuizId { get; set; }

    [Required(ErrorMessage = "QuizRating is required.")]
    [Range(1, 5, ErrorMessage = "QuizRating must be between 1 and 5.")]
    public int QuizRating { get; set; }
    
    public string? Feedback { get; set; }
}
