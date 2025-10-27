using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs
{
    public class QuizDifficultyRequestDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Difficulty Name is required.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z ]*$", ErrorMessage = "Difficulty Name must contain only alphabets")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9 .,!?\-_@#]*$", ErrorMessage = "Quiz Difficulty Description must start with letter/number.")]
        public string Description { get; set; } = string.Empty;
    }
}
