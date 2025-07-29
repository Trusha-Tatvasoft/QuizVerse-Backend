using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface
{
    public interface IAuthService
    {
        Task<(string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto);

        Task<(string accessToken, string refreshToken)> ValidateRefreshTokens(string refreshToken);

        Task<UserDto> RegisterUser(UserRegisterDto userRegisterDto);
    }
}