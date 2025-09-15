using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs
{
    public class TemplatedEmailRequestDto
    {
        public required string ToEmail { get; set; }
        public required EmailTemplateType TemplateType { get; set; }
        public required Dictionary<string, string> Placeholders { get; set; }
    }
}
