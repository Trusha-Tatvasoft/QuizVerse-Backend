using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class AuthService(ITokenService _tokenService, ICommonService _commonService, IGenericRepository<User> _genericUserRepository, IGenericRepository<PasswordResetToken> _genericPasswordResetTokenRepository, IConfiguration _config, IEmailService _emailService) : IAuthService
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

            if (!_commonService.VerifyPassword(userLoginDto.Password, user.Password))
            {
                throw new ArgumentException(Constants.INVALID_PASSWORD_MESSAGE);
            }

            string accessToken = _tokenService.GenerateAccessToken(user);
            string refreshToken = _tokenService.GenerateRefreshToken(user, userLoginDto.RememberMe);
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
                bool rememberMe = _tokenService.IsRememberMeEnabled(principal);
                if (!rememberMe)
                {
                    throw new AppException(Constants.EXPIRED_LOGIN_SESSION_MESSAGE, StatusCodes.Status401Unauthorized);
                }
            }

            string newAccessToken = _tokenService.GenerateAccessToken(user);
            string newRefreshToken = _tokenService.GenerateRefreshToken(user, _tokenService.IsRememberMeEnabled(principal));

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

        public async Task<bool> ResetPasswordMail(string emailId)
        {
            User? user = await _genericUserRepository.GetAsync(u => u.Email == emailId && u.IsDeleted == false);
            if (user == null)
            {
                throw new AppException(Constants.USER_NOT_FOUND_MESSAGE);
            }
            if (user.Status == (int)UserStatus.Inactive)
            {
                throw new ArgumentException(Constants.INACTIVE_USER_MESSAGE);
            }
            string resetPasswordToken = _tokenService.GenerateResetPasswordToken(user);
            PasswordResetToken passwordResetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetPasswordToken,
                ExpireAt = DateTime.UtcNow.AddMinutes(double.Parse(_config["ResetPasswordTokenExpiryMinutes"]!)),
                IsUsed = false
            };
            await _genericPasswordResetTokenRepository.AddAsync(passwordResetToken);
            if (passwordResetToken.TokenId <= 0 || string.IsNullOrEmpty(passwordResetToken.Token))
            {
                throw new AppException(Constants.FAILED_TO_CREATE_RESET_PASSWORD_TOKEN);
            }
            bool emailSent = await SendMailForResetPassword(resetPasswordToken, user.FullName, emailId);
            if (!emailSent)
            {
                throw new AppException(Constants.EMAIL_NOT_SENT);
            }
            return true;
        }

        private async Task<bool> SendMailForResetPassword(string resetPasswordToken, string userName, string emailId)
        {
            string baseUrl = _config["baseUrl"]!;
            string resetLink = $"{baseUrl}/{Constants.RESET_PASSWORD_FE_PATH}?token={resetPasswordToken}";
            string templatePath = _config["EmailSettings:ResetPasswordTemplatePath"]!;
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);
            string emailBody = await File.ReadAllTextAsync(fullPath);

            emailBody = emailBody.Replace("{userName}", userName);
            emailBody = emailBody.Replace("{userEmail}", emailId);
            emailBody = emailBody.Replace("{resetLink}", resetLink);

            EmailRequestDto mail = new EmailRequestDto
            {
                To = emailId,
                Subject = Constants.RESET_PASSWORD_EMAIL_HEADING,
                Body = emailBody
            };
            return await _emailService.SendEmailAsync(mail);
        }

        public async Task<bool> ResetPasswordTokenValidation(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException(Constants.EMPTY_TOKEN_MESSAGE);
            }
            ClaimsPrincipal principal = _tokenService.ValidateToken(token, validateLifetime: true);
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                throw new ArgumentException(Constants.INVALID_RESET_PASSWORD_TOKEN);
            }
            string userIdStr = _tokenService.GetUserIdFromToken(principal);
            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new ArgumentException(Constants.INVALID_USER_ID_MESSAGE);
            }
            PasswordResetToken passwordResetToken = await _genericPasswordResetTokenRepository.GetAsync(
                t => t.Token == token && (bool)!t.IsUsed && t.ExpireAt > DateTime.UtcNow)
                ?? throw new ArgumentException(Constants.INVALID_RESET_PASSWORD_TOKEN);
            User? user = await _genericUserRepository.GetAsync(u => u.Id == userId && !u.IsDeleted)
                ?? throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);
            if (user.Status == (int)UserStatus.Inactive)
            {
                throw new ArgumentException(Constants.INACTIVE_USER_MESSAGE);
            }
            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordDTO resetPasswordDto)
        {
            if (resetPasswordDto == null || string.IsNullOrEmpty(resetPasswordDto.ResetPasswordToken) || string.IsNullOrEmpty(resetPasswordDto.Password))
            {
                throw new ArgumentException(Constants.INVALID_DATA_MESSAGE);
            }
            PasswordResetToken passwordResetToken = await _genericPasswordResetTokenRepository.GetAsync(
               t => t.Token == resetPasswordDto.ResetPasswordToken && (bool)!t.IsUsed && t.ExpireAt > DateTime.UtcNow)
               ?? throw new ArgumentException(Constants.INVALID_RESET_PASSWORD_TOKEN);
            ClaimsPrincipal principal = _tokenService.ValidateToken(passwordResetToken.Token, validateLifetime: true);
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                throw new ArgumentException(Constants.INVALID_RESET_PASSWORD_TOKEN);
            }
            string userIdStr = _tokenService.GetUserIdFromToken(_tokenService.ValidateToken(resetPasswordDto.ResetPasswordToken, validateLifetime: true));
            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new ArgumentException(Constants.INVALID_USER_ID_MESSAGE);
            }
            User? user = await _genericUserRepository.GetAsync(u => u.Id == userId && !u.IsDeleted)
                ?? throw new ArgumentException(Constants.USER_NOT_FOUND_MESSAGE);
            if (user.Status == (int)UserStatus.Inactive)
            {
                throw new ArgumentException(Constants.INACTIVE_USER_MESSAGE);
            }
            user.Password = _commonService.Hash(resetPasswordDto.Password);
            passwordResetToken.IsUsed = true;
            await _genericUserRepository.UpdateAsync(user);
            await _genericPasswordResetTokenRepository.UpdateAsync(passwordResetToken);
            return true;
        } 
    }
}