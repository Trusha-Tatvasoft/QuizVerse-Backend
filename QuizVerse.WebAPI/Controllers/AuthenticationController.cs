using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController(IAuthService authService) : ControllerBase
    {
        public readonly IAuthService _authService = authService;

        [HttpPost("login", Name = "Login")]
        public async Task<IActionResult> Login(UserLoginDTO userLoginDTO)
        {
            (string accessToken, string refreshToken) = await _authService.AuthenticateUser(userLoginDTO);
            ApiResponse<LoginResponseDTO> response = new()
            {
                Result = true,
                Message = "Logged in successfully",
                StatusCode = StatusCodes.Status200OK,
                Data = new LoginResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                },
            };

            return Ok(response);
        }


        [HttpPost("refersh-token", Name = "ValidateAndRegenerateRefreshToken")]
        public async Task<IActionResult> ValidateAndRegenerateRefreshToken(string refereshToken)
        {
            (string accessToken, string refreshToken) = await _authService.ValidateRefreshTokens(refereshToken);
            ApiResponse<LoginResponseDTO> response = new()
            {
                Result = true,
                Message = "Tokens regenerated successfully",
                StatusCode = StatusCodes.Status200OK,
                Data = new LoginResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                },
            };

            return Ok(response);
        }
    }
}