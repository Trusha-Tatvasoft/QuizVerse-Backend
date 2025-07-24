using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Application.Core.Interface
{
    public interface IAuthService
    {
        Task<(string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto);

        Task<(string accessToken, string refreshToken)> ValidateRefreshTokens(string refreshToken);
    }
}