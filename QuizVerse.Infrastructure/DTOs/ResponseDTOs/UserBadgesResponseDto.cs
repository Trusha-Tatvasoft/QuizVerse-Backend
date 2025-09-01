using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class UserBadgesResponseDto
{
    public int BadgeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Earned { get; set; }
    public BadgeType BadgeType { get; set; } 
}
