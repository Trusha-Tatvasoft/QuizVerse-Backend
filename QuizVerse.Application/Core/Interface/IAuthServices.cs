using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Application.Core.Interface
{
    public interface IAuthService
    {
        Task<(User? user, string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto);
    }
}