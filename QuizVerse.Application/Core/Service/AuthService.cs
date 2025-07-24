using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly ICustomService _customService;
        private readonly IGenericRepository<User> _genericUserRepository;

        public AuthService(ITokenService tokenService,ICustomService customService, IGenericRepository<User> genericUserRepository)
        {
            _tokenService = tokenService;
            _customService = customService;
            _genericUserRepository = genericUserRepository;
        }

        public async Task<(string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto)
        {
            if (userLoginDto == null || string.IsNullOrEmpty(userLoginDto.Email) || string.IsNullOrEmpty(userLoginDto.Password))
            {
                throw new ArgumentException("Empty User Login Details.");
            }

            User? user = await _genericUserRepository.GetAsync(u => u.Email.ToLower() == userLoginDto.Email.ToLower() && !u.IsDeleted) ?? throw new ArgumentException("User not found.");

            if (user.Status != (int)UserStatus.Active)
            {
                throw new ArgumentException("User is not active. PLease Ask Admin to Activate your Account.");
            }

            /* Check if first time login user then send to register user page (remain do after the reset-password gets completed)*/

            if (!_customService.VerifyPassword(userLoginDto.Password, user.Password))
            {
                throw new ArgumentException("Invalid password.");
            }

            string accessToken = _tokenService.GenerateAccessTokenAsync(user);
            string refreshToken = _tokenService.GenerateRefreshTokenAsync(user, userLoginDto.RememberMe);
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                throw new Exception("Failed to generate tokens.");
            }
            user.LastLogin = DateTime.UtcNow;

            await _genericUserRepository.UpdateAsync(user);

            return (accessToken, refreshToken);
        }

        public async Task<(string accessToken, string refreshToken)> ValidateRefreshTokens(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentException("Refresh token is required.");
            }

            ClaimsPrincipal principal;
            bool isExpired = false;

            try
            {
                principal = _tokenService.ValidateToken(refreshToken, validateLifetime: true);
            }
            catch (AppException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized && ex.Message.Contains("expired"))
            {
                principal = _tokenService.ValidateToken(refreshToken, validateLifetime: false);
                isExpired = true;
            }

            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                throw new ArgumentException("Invalid refresh token.");
            }

            var userIdStr = _tokenService.GetUserIdFromToken(principal);
            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new ArgumentException("Invalid user ID in refresh token.");
            }

            User? user = await _genericUserRepository.GetAsync(u => u.Id == userId && !u.IsDeleted)
                ?? throw new ArgumentException("User not found.");

            if (user.Status != (int)UserStatus.Active)
            {
                throw new ArgumentException("User is not active. Please contact the administrator.");
            }

            if (isExpired)
            {
                bool rememberMe = _tokenService.IsRememberMeEnabled(principal);
                if (!rememberMe)
                {
                    throw new AppException("Your Login Session has expired. Please login again.", StatusCodes.Status401Unauthorized);
                }
            }

            string newAccessToken = _tokenService.GenerateAccessTokenAsync(user);
            string newRefreshToken = _tokenService.GenerateRefreshTokenAsync(user, _tokenService.IsRememberMeEnabled(principal));

            if (string.IsNullOrEmpty(newAccessToken) || string.IsNullOrEmpty(newRefreshToken))
            {
                throw new Exception("Failed to generate new tokens.");
            }

            user.LastLogin = DateTime.UtcNow;
            await _genericUserRepository.UpdateAsync(user);

            return (newAccessToken, newRefreshToken);
        }

    }
}