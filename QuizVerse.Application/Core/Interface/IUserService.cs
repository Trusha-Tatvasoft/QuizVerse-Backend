using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IUserService
{
    Task<UserDto> GetUserById(int id);
    Task<(bool Success, string Message)> CreateOrUpdateUser(UserRequestDto dto);
    Task<string> UpdateUserByAction(UserActionRequest request);
}
