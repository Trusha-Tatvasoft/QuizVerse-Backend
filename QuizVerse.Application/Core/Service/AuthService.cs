using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly ICustomService _customService;
        private readonly IGenericRepository<User> _genericUserRepository;

        public AuthService(ITokenService tokenService, ICustomService customService, IGenericRepository<User> genericUserRepository)
        {
            _tokenService = tokenService;
            _customService = customService;
            _genericUserRepository = genericUserRepository;
        }

        public async Task<(string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto)
        {
            if (userLoginDto == null || string.IsNullOrEmpty(userLoginDto.Email) || string.IsNullOrEmpty(userLoginDto.Password))
            {
                throw new ArgumentException(Constants.INVALID_LOGIN_CREDENTIALS_MESSAGE);
            }

            User? user = await _genericUserRepository.GetAsync(u => u.Email.ToLower() == userLoginDto.Email.ToLower() && !u.IsDeleted,
    query => query.Include(u => u.Role)) ?? throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);

            if (user.Status != (int)UserStatus.Active)
            {
                if (user.Status == (int)UserStatus.Inactive)
                {
                    throw new ArgumentException(Constants.INACTIVE_USER_MESSAGE);
                }
                else
                {
                    DateTime? ModifiedDate = user.ModifiedDate;
                    DateTime currentTime = DateTime.UtcNow;
                    TimeSpan elapsed = currentTime - (ModifiedDate ?? throw new ArgumentException(Constants.NULL_MODIFIED_DATE_MESSAGE));

                    TimeSpan totalDuration = TimeSpan.FromDays(30);
                    TimeSpan remaining = totalDuration - elapsed;
                    if (remaining > TimeSpan.Zero)
                    {
                        int remainingDays = remaining.Days;
                        int remainingHours = remaining.Hours;
                        throw new ArgumentException($"You have been suspended. Remaining suspension time: {remainingDays} days and {remainingHours} hours.");
                    }
                    else
                    {
                        user.Status = (int)UserStatus.Active;
                    }
                }
            }

            if (!_customService.VerifyPassword(userLoginDto.Password, user.Password))
            {
                throw new ArgumentException(Constants.INVALID_PASSWORD_MESSAGE);
            }

            string accessToken = _tokenService.GenerateAccessTokenAsync(user);
            string refreshToken = _tokenService.GenerateRefreshTokenAsync(user, userLoginDto.RememberMe);
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                throw new Exception(Constants.FAILED_TOKEN_GENERATION_MESSAGE);
            }
            user.LastLogin = DateTime.UtcNow;

            await _genericUserRepository.UpdateAsync(user);

            return (accessToken, refreshToken);
        }

        public async Task<(string accessToken, string refreshToken)> ValidateRefreshTokens(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentException(Constants.REFRESH_TOKEN_REQUIRED_MESSAGE);
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
                throw new ArgumentException(Constants.INVALID_DATA_MESSAGE);
            }

            var userIdStr = _tokenService.GetUserIdFromToken(principal);
            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new ArgumentException(Constants.INVALID_USER_ID_MESSAGE);
            }

            User? user = await _genericUserRepository.GetAsync(u => u.Id == userId && !u.IsDeleted,
                query => query.Include(u => u.Role))
                ?? throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);

            if (user.Status != (int)UserStatus.Active)
            {
                if (user.Status == (int)UserStatus.Inactive)
                {
                    throw new ArgumentException(Constants.INACTIVE_USER_MESSAGE);
                }
                else
                {
                    DateTime? ModifiedDate = user.ModifiedDate;
                    DateTime currentTime = DateTime.UtcNow;
                    TimeSpan elapsed = currentTime - (ModifiedDate ?? throw new ArgumentException(Constants.NULL_MODIFIED_DATE_MESSAGE));

                    TimeSpan totalDuration = TimeSpan.FromDays(30);
                    TimeSpan remaining = totalDuration - elapsed;

                    int remainingDays = remaining.Days;
                    int remainingHours = remaining.Hours;
                    throw new ArgumentException(string.Format(Constants.USER_SUSPENDED_MESSAGE, remainingDays, remainingHours));
                }
            }

            if (isExpired)
            {
                bool rememberMe = _tokenService.IsRememberMeEnabled(principal);
                if (!rememberMe)
                {
                    throw new AppException(Constants.EXPIRED_LOGIN_SESSION_MESSAGE, StatusCodes.Status401Unauthorized);
                }
            }

            string newAccessToken = _tokenService.GenerateAccessTokenAsync(user);
            string newRefreshToken = _tokenService.GenerateRefreshTokenAsync(user, _tokenService.IsRememberMeEnabled(principal));

            if (string.IsNullOrEmpty(newAccessToken) || string.IsNullOrEmpty(newRefreshToken))
            {
                throw new Exception(Constants.FAILED_TOKEN_GENERATION_MESSAGE);
            }

            user.LastLogin = DateTime.UtcNow;
            await _genericUserRepository.UpdateAsync(user);

            return (newAccessToken, newRefreshToken);
        }

    }
}