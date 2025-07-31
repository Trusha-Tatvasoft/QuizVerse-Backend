using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IUserService
{
    Task<PageListResponse<UserDto>> GetUsersByPagination(PageListRequest query);
    Task<UserDto> GetUserById(int id);
    Task<(bool Success, string Message)> CreateOrUpdateUser(UserRequestDto dto);
    Task<string> UpdateUserByAction(UserActionRequest request);
    Task<MemoryStream> UserExportData(PageListRequest query);
}
