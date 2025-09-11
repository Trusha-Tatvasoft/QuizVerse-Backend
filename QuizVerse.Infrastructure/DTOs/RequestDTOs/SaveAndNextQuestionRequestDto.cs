using System.ComponentModel.DataAnnotations;
namespace QuizVerse.Infrastructure.DTOs.RequestDTOs
{
    public class SaveAndNextQuestionRequestDto
    {
        [Required]
        public int QuizId { get; set; }
        [Required]
        public int CurrentQuestionId { get; set; }
        public string GivenAnswer { get; set; } = string.Empty;
        [Required]
        public int NextQuestionNumber { get; set; }
    }
}
