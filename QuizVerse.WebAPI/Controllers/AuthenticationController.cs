using Microsoft.AspNetCore.Mvc;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        public readonly IAuthService _authService;

        public AuthenticationController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login", Name = "Login")]
        public async Task<IActionResult> Login(UserLoginDTO userLoginDTO)
        {
            (User? user, string accessToken, string refreshToken) = await _authService.AuthenticateUser(userLoginDTO);
            ApiResponse<LoginResponseDTO> response = new()
            {
                Result = true,
                Message = "Logged in successfully",
                StatusCode = StatusCodes.Status200OK,
                Data = new LoginResponseDTO
                {
                    User = user,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                },
            };

            return Ok(response);
        }
    }
}