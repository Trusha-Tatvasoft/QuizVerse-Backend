using System.ComponentModel.DataAnnotations;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class LandingPageData
{
    [Required]
    public string Quote { get; set; } = string.Empty;

    public long ActivePlayer { get; set; }

    public long QuizCreated { get; set; }

    public long QuestionAns { get; set; }
}
