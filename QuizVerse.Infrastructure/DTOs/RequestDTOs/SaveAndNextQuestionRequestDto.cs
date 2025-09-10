using System.ComponentModel.DataAnnotations;
namespace QuizVerse.Infrastructure.DTOs.RequestDTOs
{
    public class SaveAndNextQuestionRequestDto
    {
        [Required]
        public int QuizId { get; set; }

        [Required]
        public int CurrentQuestionId { get; set; }

        [Required(ErrorMessage = "Answer is required.")]
        public string GivenAnswer { get; set; } = null!;

        public int NextQuestionNumber { get; set; }
    }
}
