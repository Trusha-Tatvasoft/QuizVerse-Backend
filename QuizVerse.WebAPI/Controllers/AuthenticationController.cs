using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController(IAuthService authService) : ControllerBase
    {
        public readonly IAuthService _authService = authService;

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO userLoginDTO)
        {
            (string accessToken, string refreshToken) = await _authService.AuthenticateUser(userLoginDTO);
            ApiResponse<LoginResponseDTO> response = new()
            {
                Result = true,
                Message = Constants.USER_LOGIN_SUCCESS_MESSAGE,
                StatusCode = StatusCodes.Status200OK,
                Data = new LoginResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                },
            };

            return Ok(response);
        }


        [HttpPost("refersh-token")]
        public async Task<IActionResult> ValidateAndRegenerateRefreshToken(string refereshToken)
        {
            (string accessToken, string refreshToken) = await _authService.ValidateRefreshTokens(refereshToken);
            ApiResponse<LoginResponseDTO> response = new()
            {
                Result = true,
                Message = Constants.VALIDATE_AND_REGENERATE_REFERESH_TOKEN_SUCCESS_MESSAGE,
                StatusCode = StatusCodes.Status200OK,
                Data = new LoginResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                },
            };

            return Ok(response);
        }


        [HttpGet("reset-password-mail")]
        public async Task<IActionResult> ResetPasswordMail(string emailId)
        {
            ApiResponse<bool> response = new ApiResponse<bool>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.SEND_MAIL_SUCCESS_MESSAGE,
                Data = await authService.ResetPasswordMail(emailId),
            };
            return Ok(response);
        }


        [HttpGet("reset-password-token-validation")]
        public async Task<IActionResult> ResetPasswordTokenValidation(string resetPasswordToken)
        {
            ApiResponse<bool> response = new ApiResponse<bool>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.VALID_RESET_PASSWORD_TOKEN,
                Data = await authService.ResetPasswordTokenValidation(resetPasswordToken),
            };
            return Ok(response);
        }
        

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPasswordDto)
        {
            ApiResponse<bool> response = new ApiResponse<bool>
            {
                Result = true,
                StatusCode = StatusCodes.Status200OK,
                Message = Constants.PASSWORD_UPDATE_SUCCESS_MESSAGE,
                Data = await authService.ResetPassword(resetPasswordDto),
            };
            return Ok(response);
        }
    }
}