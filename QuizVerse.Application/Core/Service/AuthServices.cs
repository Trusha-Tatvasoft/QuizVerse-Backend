using BCrypt.Net;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class AuthService : IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly IGenericRepository<User> _genericUserRepository;

        public AuthService(ITokenService tokenService, IGenericRepository<User> genericUserRepository)
        {
            _tokenService = tokenService;
            _genericUserRepository = genericUserRepository;
        }

        public async Task<(User? user, string accessToken, string refereshToken)> AuthenticateUser(UserLoginDTO userLoginDto)
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

            if (!BCrypt.Net.BCrypt.Verify(userLoginDto.Password, user.Password))
            {
                throw new ArgumentException("Invalid password.");
            }

            string accessToken = _tokenService.GenerateAccessTokenAsync(user);
            string refreshToken = _tokenService.GenerateRefreshTokenAsync();
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                throw new Exception("Failed to generate tokens.");
            }
            user.LastLogin = DateTime.UtcNow;

            await _genericUserRepository.UpdateAsync(user);

            return (user, accessToken, refreshToken);
        }
    }
}