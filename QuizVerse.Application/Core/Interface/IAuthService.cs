using QuizVerse.Infrastructure.DTOs.RequestDTOs;

namespace QuizVerse.Application.Core.Interface
{
    public interface IAuthService
    {
        Task<(string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto);

        Task<(string accessToken, string refreshToken)> ValidateRefreshTokens(string refreshToken);

        public Task<(bool success, string message)> RegisterUser(UserRegisterDto userRegisterDto);

        Task<bool> ForgotPassword(string email);

        Task<bool> VerifyTokenResetPassword(string token);

        Task<bool> ResetPassword(ResetPasswordDTO resetPasswordDto);
    }
}