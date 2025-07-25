using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Application.Core.Interface;

public interface IUserService
{
    Task<UserDto> GetUserById(int id);
    Task<UserDto> CreateUser(CreateUserDto dto);
    Task<UserDto> UpdateUser(int id, UpdateUserDto dto);
    Task DeleteUser(int id);
    Task ChangeUserStatus(int id, UserStatus newStatus);
}
