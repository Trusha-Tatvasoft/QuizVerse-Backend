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
    public class AuthService(ITokenService tokenService, ICommonService _customService, IGenericRepository<User> _genericUserRepository) : IAuthService
    {
        public async Task<(string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto)
        {
            if (userLoginDto == null || string.IsNullOrEmpty(userLoginDto.Email) || string.IsNullOrEmpty(userLoginDto.Password))
            {
                throw new ArgumentException(Constants.INVALID_LOGIN_CREDENTIALS_MESSAGE);
            }

            User? user = await _genericUserRepository.GetAsync(u => u.Email.ToLower() == userLoginDto.Email.ToLower() && !u.IsDeleted,
    query => query.Include(u => u.Role)) ?? throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);

            if (user.Status == (int)UserStatus.Inactive)
            {
                throw new ArgumentException(Constants.INACTIVE_USER_MESSAGE);
            }
            else if (user.Status == (int)UserStatus.Suspended)
            {
                TimeSpan remaining = CalculateSuspensionRemainingTime(user);

                if (remaining > TimeSpan.Zero)
                {
                    int remainingDays = remaining.Days;
                    int remainingHours = remaining.Hours;
                    throw new ArgumentException(string.Format(Constants.USER_SUSPENDED_MESSAGE, remainingDays, remainingHours));
                }
                else
                {
                    user.Status = (int)UserStatus.Active;
                }
            }

            if (!_customService.VerifyPassword(userLoginDto.Password, user.Password))
            {
                throw new ArgumentException(Constants.INVALID_PASSWORD_MESSAGE);
            }

            string accessToken = tokenService.GenerateAccessToken(user);
            string refreshToken = tokenService.GenerateRefreshToken(user, userLoginDto.RememberMe);
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
                principal = tokenService.ValidateToken(refreshToken, validateLifetime: true);
            }
            catch (AppException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized && ex.Message.Contains("expired"))
            {
                principal = tokenService.ValidateToken(refreshToken, validateLifetime: false);
                isExpired = true;
            }

            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                throw new ArgumentException(Constants.INVALID_DATA_MESSAGE);
            }

            var userIdStr = tokenService.GetUserIdFromToken(principal);
            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new ArgumentException(Constants.INVALID_USER_ID_MESSAGE);
            }

            User? user = await _genericUserRepository.GetAsync(u => u.Id == userId && !u.IsDeleted,
                query => query.Include(u => u.Role))
                ?? throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);

            if (user.Status == (int)UserStatus.Inactive)
            {
                throw new ArgumentException(Constants.INACTIVE_USER_MESSAGE);
            }
            else if (user.Status == (int)UserStatus.Suspended)
            {

                TimeSpan remaining = CalculateSuspensionRemainingTime(user);

                if (remaining > TimeSpan.Zero)
                {
                    int remainingDays = remaining.Days;
                    int remainingHours = remaining.Hours;
                    throw new ArgumentException(string.Format(Constants.USER_SUSPENDED_MESSAGE, remainingDays, remainingHours));
                }
                else
                {
                    user.Status = (int)UserStatus.Active;
                }
            }

            if (isExpired)
            {
                bool rememberMe = tokenService.IsRememberMeEnabled(principal);
                if (!rememberMe)
                {
                    throw new AppException(Constants.EXPIRED_LOGIN_SESSION_MESSAGE, StatusCodes.Status401Unauthorized);
                }
            }

            string newAccessToken = tokenService.GenerateAccessToken(user);
            string newRefreshToken = tokenService.GenerateRefreshToken(user, tokenService.IsRememberMeEnabled(principal));

            if (string.IsNullOrEmpty(newAccessToken) || string.IsNullOrEmpty(newRefreshToken))
            {
                throw new Exception(Constants.FAILED_TOKEN_GENERATION_MESSAGE);
            }

            user.LastLogin = DateTime.UtcNow;
            await _genericUserRepository.UpdateAsync(user);

            return (newAccessToken, newRefreshToken);
        }

        private static TimeSpan CalculateSuspensionRemainingTime(User user)
        {
            DateTime? modifiedDate = user.ModifiedDate ?? throw new ArgumentException(Constants.NULL_MODIFIED_DATE_MESSAGE);
            DateTime currentTime = DateTime.UtcNow;
            TimeSpan elapsed = currentTime - modifiedDate.Value;

            TimeSpan totalDuration = TimeSpan.FromDays(30);
            return totalDuration - elapsed;
        }

    }
}