using Xunit;
using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.UnitTests.Services
{
    public class TokenServiceTests
    {
        private readonly IConfiguration _configuration;

        public TokenServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string?> {
        { "JwtSettings:Key", "super_secret_jwt_key_for_testing_1234567890" },
        { "JwtSettings:Issuer", "QuizVerseIssuer" },
        { "JwtSettings:Audience", "QuizVerseAudience" },
        { "AccessTokenExpiryMinutes", "30" },
        { "RefreshTokenExpiryDays", "7" },
        { "ResetPasswordTokenExpiryMinutes", "15" }
    };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }


        private TokenService CreateService() => new TokenService(_configuration);

        private User CreateTestUser() =>
            new User
            {
                Id = 1,
                Email = "testuser@example.com",
                Role = new Domain.Entities.UserRole { Id = 2, Name = "Player" }
            };

        [Fact]
        public void GenerateAccessToken_Returns_ValidToken()
        {
            var service = CreateService();
            var user = CreateTestUser();

            var token = service.GenerateAccessToken(user);

            Assert.False(string.IsNullOrEmpty(token));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal(_configuration["JwtSettings:Issuer"], jwtToken.Issuer);
            Assert.Equal(_configuration["JwtSettings:Audience"], jwtToken.Audiences.First());

            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.UserData && c.Value == user.Id.ToString());
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == user.Role.Name);
        }

        [Fact]
        public void GenerateRefreshToken_Returns_ValidToken_WithRememberMe()
        {
            var service = CreateService();
            var user = CreateTestUser();
            bool rememberMe = true;

            var token = service.GenerateRefreshToken(user, rememberMe);

            Assert.False(string.IsNullOrEmpty(token));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal(_configuration["JwtSettings:Issuer"], jwtToken.Issuer);
            Assert.Equal(_configuration["JwtSettings:Audience"], jwtToken.Audiences.First());

            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.UserData && c.Value == user.Id.ToString());
            Assert.Contains(jwtToken.Claims, c => c.Type == SystemConstants.REMEMBER_ME_CLAIM_NAME && c.Value == rememberMe.ToString());
        }


        [Fact]
        public void ValidateToken_ValidToken_ReturnsPrincipal()
        {
            var service = CreateService();
            var user = CreateTestUser();
            var token = service.GenerateAccessToken(user);

            var principal = service.ValidateToken(token);

            Assert.NotNull(principal);
            Assert.True(principal.Identity!.IsAuthenticated);
            Assert.Equal(user.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.UserData)?.Value);
            Assert.Equal(user.Role.Name, principal.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public void ValidateToken_InvalidToken_ThrowsAppException()
        {
            // Arrange
            var service = CreateService();
            var invalidToken = "invalid.token.value";

            // Act & Assert
            var ex = Assert.Throws<AppException>(() => service.ValidateToken(invalidToken));
            Assert.Equal(Constants.INVALID_TOKEN_FORMAT_MESSAGE, ex.Message);
        }

        [Fact]
        public void IsRememberMeEnabled_Returns_CorrectValue()
        {
            var service = CreateService();
            var user = CreateTestUser();
            var token = service.GenerateRefreshToken(user, true);

            var principal = service.ValidateToken(token);

            var result = service.IsRememberMeEnabled(principal);

            Assert.True(result);
        }

        [Fact]
        public void GetUserIdFromToken_Returns_CorrectUserId()
        {
            var service = CreateService();
            var user = CreateTestUser();
            var token = service.GenerateAccessToken(user);
            var principal = service.ValidateToken(token);

            var userId = service.GetUserIdFromToken(principal);

            Assert.Equal(user.Id.ToString(), userId);
        }

        [Fact]
        public void GetTokenExpiration_Returns_CorrectExpiration()
        {
            var service = CreateService();
            var user = CreateTestUser();
            var token = service.GenerateAccessToken(user);

            var expiration = service.GetTokenExpiration(token);

            Assert.True(expiration > DateTime.UtcNow);
        }

        [Fact]
        public void GenerateAccessToken_Throws_WhenUserIsNull()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => service.GenerateAccessToken(null));
            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public void ValidateToken_ExpiredToken_ThrowsAppException()
        {
            // Arrange
            var service = CreateService();
            var user = CreateTestUser();

            // Create a token with past expiration
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(-1), // already expired
                signingCredentials: creds
            );

            var handler = new JwtSecurityTokenHandler();
            var expiredToken = handler.WriteToken(token);

            // Act & Assert
            var ex = Assert.Throws<AppException>(() => service.ValidateToken(expiredToken));
            Assert.Equal(Constants.EXPIRED_TOKEN_MESSAGE, ex.Message);
        }

        [Fact]
        public void GenerateResetPasswordToken_Returns_ValidToken()
        {
            // Arrange
            var service = CreateService();
            var user = CreateTestUser();

            var token = service.GenerateResetPasswordToken(user);

            Assert.False(string.IsNullOrEmpty(token));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            Assert.Equal(_configuration["JwtSettings:Issuer"], jwtToken.Issuer);
            Assert.Equal(_configuration["JwtSettings:Audience"], jwtToken.Audiences.First());

            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.UserData && c.Value == user.Id.ToString());
        }
    }
}
