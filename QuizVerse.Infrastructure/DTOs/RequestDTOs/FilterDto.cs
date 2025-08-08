using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class FilterDto
{
    public UserStatus? Status { get; set; }
    public UserRoles? Role { get; set; }
    public QuizStatus? QuizStatus { get; set; }
    public int? QuizCategoryId { get; set; }
    public int? QuizDifficultyId { get; set; }
    public int? QuestionDifficultyId { get; set; }
    public int? QuestionTypeId { get; set; }
}
