using QuizVerse.Infrastructure.DTOs.RequestDTOs;

namespace QuizVerse.Application.Core.Interface
{
    public interface IAuthService
    {
        Task<(string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto);

        Task<(string accessToken, string refreshToken)> ValidateRefreshTokens(string refreshToken);

        Task<bool> ResetPasswordMail(string mail);

        Task<bool> ResetPasswordTokenValidation(string token);

        Task<bool> ResetPassword(ResetPasswordDTO resetPasswordDto);
    }
}