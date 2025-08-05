using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class FilterDto
{
    public UserStatus? Status { get; set; }
    public UserRoles? Role { get; set; }
}
