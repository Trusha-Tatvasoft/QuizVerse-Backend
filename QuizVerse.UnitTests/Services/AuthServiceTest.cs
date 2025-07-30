using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;
using System.Linq.Expressions;
using QuizVerse.Infrastructure.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace QuizVerse.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ICommonService> _commonServiceMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IGenericRepository<User>> _userRepoMock = new();
        private readonly Mock<IGenericRepository<PasswordResetToken>> _passwordResetTokenRepoMock = new();

        private AuthService CreateService() =>
            new AuthService(_tokenServiceMock.Object, _commonServiceMock.Object, _userRepoMock.Object, _passwordResetTokenRepoMock.Object, _configMock.Object, _emailServiceMock.Object);

        private User CreateTestUser(int status = (int)UserStatus.Active, DateTime? modifiedDate = null)
        {
            return new User
            {
                Id = 1,
                Email = "test@example.com",
                Password = "hashed_password",
                Status = status,
                ModifiedDate = modifiedDate ?? DateTime.UtcNow.AddDays(-31),
                Role = new Domain.Entities.UserRole { Id = 2, Name = "Player" }
            };
        }

        private ClaimsPrincipal CreateClaimsPrincipal(int userId, bool isAuthenticated = true)
        {
            var identity = new ClaimsIdentity(
                isAuthenticated ? new[]
                {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("remember_me", "true")
                } : null,
                isAuthenticated ? "TestAuthType" : null
            );

            return new ClaimsPrincipal(identity);
        }


        [Fact]
        public async Task AuthenticateUser_ReturnsTokens_WhenCredentialsAreValid()
        {
            var userDto = new UserLoginDTO { Email = "test@example.com", Password = "pass", RememberMe = false };
            var user = CreateTestUser();

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                                                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);
            _commonServiceMock.Setup(s => s.VerifyPassword(userDto.Password, user.Password)).Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user, userDto.RememberMe)).Returns("refresh_token");

            var service = CreateService();
            var (accessToken, refreshToken) = await service.AuthenticateUser(userDto);

            Assert.Equal("access_token", accessToken);
            Assert.Equal("refresh_token", refreshToken);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenUserLoginDtoIsNull()
        {
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(null!));
            Assert.Equal(Constants.INVALID_LOGIN_CREDENTIALS_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenEmailOrPasswordEmpty()
        {
            var service = CreateService();

            var dto1 = new UserLoginDTO { Email = "", Password = "abc" };
            var dto2 = new UserLoginDTO { Email = "abc@test.com", Password = "" };

            var ex1 = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto1));
            var ex2 = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto2));

            Assert.Equal(Constants.INVALID_LOGIN_CREDENTIALS_MESSAGE, ex1.Message);
            Assert.Equal(Constants.INVALID_LOGIN_CREDENTIALS_MESSAGE, ex2.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenUserNotFound()
        {
            var dto = new UserLoginDTO { Email = "test@example.com", Password = "pass" };
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                                                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync((User?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenUserInactive()
        {
            var user = CreateTestUser((int)UserStatus.Inactive);
            var dto = new UserLoginDTO { Email = "test@example.com", Password = "pass" };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                                                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenUserSuspended_WithRemainingTime()
        {
            var user = CreateTestUser((int)UserStatus.Suspended, DateTime.UtcNow.AddDays(-5));
            var dto = new UserLoginDTO { Email = "test@example.com", Password = "pass" };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                                                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Contains("You have been suspended", ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_ActivatesUser_WhenSuspensionIsOver()
        {
            var user = CreateTestUser((int)UserStatus.Suspended, DateTime.UtcNow.AddDays(-35));
            var dto = new UserLoginDTO { Email = "test@example.com", Password = "pass" };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                                                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);
            _commonServiceMock.Setup(s => s.VerifyPassword(dto.Password, user.Password)).Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user, dto.RememberMe)).Returns("refresh_token");

            var service = CreateService();
            var (accessToken, refreshToken) = await service.AuthenticateUser(dto);

            Assert.Equal("access_token", accessToken);
            Assert.Equal("refresh_token", refreshToken);
            Assert.Equal((int)UserStatus.Active, user.Status);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenPasswordIsIncorrect()
        {
            var user = CreateTestUser();
            var dto = new UserLoginDTO { Email = "test@example.com", Password = "wrong" };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                                                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);
            _commonServiceMock.Setup(s => s.VerifyPassword(dto.Password, user.Password)).Returns(false);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Equal(Constants.INVALID_PASSWORD_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenTokenGenerationFails()
        {
            var user = CreateTestUser();
            var dto = new UserLoginDTO { Email = "test@example.com", Password = "pass", RememberMe = false };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                                                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);
            _commonServiceMock.Setup(s => s.VerifyPassword(dto.Password, user.Password)).Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user, dto.RememberMe)).Returns("");

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<Exception>(() => service.AuthenticateUser(dto));
            Assert.Equal(Constants.FAILED_TOKEN_GENERATION_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ValidateRefreshTokens_Throws_WhenTokenIsNull()
        {
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateRefreshTokens(null!));
            Assert.Equal(Constants.REFRESH_TOKEN_REQUIRED_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ValidateRefreshTokens_Throws_WhenPrincipalInvalid()
        {
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns((ClaimsPrincipal)null!);
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateRefreshTokens("token"));
            Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ValidateRefreshTokens_Throws_WhenUserIdInvalid()
        {
            var identity = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "notanint"),
                new Claim("remember_me", "true")
            }, "Test");

            var principal = new ClaimsPrincipal(identity);

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("notanint");

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateRefreshTokens("token"));
            Assert.Equal(Constants.INVALID_USER_ID_MESSAGE, ex.Message);
        }


        [Fact]
        public async Task ValidateRefreshTokens_Throws_WhenUserNotFound()
        {
            var principal = CreateClaimsPrincipal(1);

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>())).ReturnsAsync((User?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateRefreshTokens("token"));

            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ValidateRefreshTokens_Throws_WhenUserInactive()
        {
            var principal = CreateClaimsPrincipal(1);
            var user = CreateTestUser((int)UserStatus.Inactive);

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>())).ReturnsAsync(user);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateRefreshTokens("token"));

            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ValidateRefreshTokens_Throws_WhenUserSuspended_WithTimeRemaining()
        {
            var principal = CreateClaimsPrincipal(1);
            var user = CreateTestUser((int)UserStatus.Suspended, DateTime.UtcNow.AddDays(-5));

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>())).ReturnsAsync(user);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateRefreshTokens("token"));
            Assert.Contains("You have been suspended", ex.Message);
        }

        [Fact]
        public async Task ValidateRefreshTokens_Throws_WhenExpiredAndNotRemembered()
        {
            var principal = CreateClaimsPrincipal(1);
            var user = CreateTestUser();

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true))
                .Throws(new AppException("expired", StatusCodes.Status401Unauthorized));

            _tokenServiceMock.Setup(t => t.ValidateToken("token", false)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _tokenServiceMock.Setup(t => t.IsRememberMeEnabled(principal)).Returns(false);

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>())).ReturnsAsync(user);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<AppException>(() => service.ValidateRefreshTokens("token"));
            Assert.Equal(Constants.EXPIRED_LOGIN_SESSION_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ValidateRefreshTokens_ReturnsNewTokens_WhenValid()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("remember_me", "true")
            }, "Test", ClaimTypes.NameIdentifier, ClaimTypes.Role);

            var principal = new ClaimsPrincipal(identity);
            var user = CreateTestUser();

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _tokenServiceMock.Setup(t => t.IsRememberMeEnabled(principal)).Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("new_access");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken(user, true)).Returns("new_refresh");

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>())).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

            var service = CreateService();
            var result = await service.ValidateRefreshTokens("token");

            Assert.Equal("new_access", result.accessToken);
            Assert.Equal("new_refresh", result.refreshToken);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsTrue_WhenValidUser()
        {
            // Arrange
            var user = CreateTestUser();

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            _tokenServiceMock.Setup(t => t.GenerateSecureToken(32)).Returns("secure_token");

            _configMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"]).Returns("30");
            _configMock.Setup(c => c["baseUrl"]).Returns("https://example.com");

            _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
                                       .Returns(Task.CompletedTask)
                                       .Callback<PasswordResetToken>(token => token.TokenId = 1);

            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>())).ReturnsAsync(true);

            string projectRoot = Directory.GetCurrentDirectory();
            string templateRelativePath = Path.Combine("Templates", "ResetPassword.html");
            string templateFullPath = Path.Combine(projectRoot, templateRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(templateFullPath)!);

            string templateContent = "Hi {userName}, use this link to reset: {resetLink}";
            await File.WriteAllTextAsync(templateFullPath, templateContent);

            try
            {
                // Act
                var service = CreateService();
                var result = await service.ForgotPassword(user.Email);

                // Assert
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(templateFullPath))
                    File.Delete(templateFullPath);
            }
        }

        [Fact]
        public async Task ForgotPassword_Throws_WhenUserNotFound()
        {
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync((User?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ForgotPassword("notfound@example.com"));

            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ForgotPassword_Throws_WhenUserInactive()
        {
            var user = CreateTestUser((int)UserStatus.Inactive);
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ForgotPassword(user.Email));

            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ForgotPassword_Throws_WhenTokenSaveFails()
        {
            var user = CreateTestUser();
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateSecureToken(32)).Returns("secure_token");
            _configMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"]).Returns("15");
            _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
             .Callback<PasswordResetToken>(t =>
             {
                 t.TokenId = 0;
                 t.Token = "";
             })
             .Returns(Task.CompletedTask);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<AppException>(() => service.ForgotPassword(user.Email));

            Assert.Equal(Constants.FAILED_TO_CREATE_RESET_PASSWORD_TOKEN, ex.Message);
        }

        [Fact]
        public async Task ForgotPassword_Throws_WhenEmailFails()
        {
            // Arrange
            var user = CreateTestUser();
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
            var fullFilePath = Path.Combine(templatePath, "ResetPassword.html");

            // Ensure the directory exists
            Directory.CreateDirectory(templatePath);

            // Create a dummy file with valid placeholders
            await File.WriteAllTextAsync(fullFilePath, "Hello {userName}, reset your password here: {resetLink}");

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateSecureToken(32)).Returns("secure_token");
            _configMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"]).Returns("30");
            _configMock.Setup(c => c["baseUrl"]).Returns("https://example.com");
            _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
                                       .Callback<PasswordResetToken>(t => t.TokenId = 1)
                                       .Returns(Task.CompletedTask);
            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>())).ReturnsAsync(false);

            var service = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => service.ForgotPassword(user.Email));
            Assert.Equal(Constants.EMAIL_NOT_SENT, ex.Message);

            // Cleanup
            if (File.Exists(fullFilePath))
                File.Delete(fullFilePath);
        }

        [Fact]
        public async Task VerifyTokenResetPassword_ReturnsTrue_WhenValid()
        {
            var user = CreateTestUser();
            var token = new PasswordResetToken { UserId = user.Id, IsUsed = false, ExpireAt = DateTime.UtcNow.AddMinutes(10) };

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                               .ReturnsAsync(token);
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            var service = CreateService();
            var result = await service.VerifyTokenResetPassword("valid_token");

            Assert.True(result);
        }

        [Fact]
        public async Task VerifyTokenResetPassword_Throws_WhenTokenEmpty()
        {
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyTokenResetPassword(""));

            Assert.Equal(Constants.EMPTY_TOKEN_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task VerifyTokenResetPassword_Throws_WhenTokenInvalid()
        {
            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                               .ReturnsAsync((PasswordResetToken?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyTokenResetPassword("invalid_token"));

            Assert.Equal(Constants.INVALID_RESET_PASSWORD_TOKEN, ex.Message);
        }

        [Fact]
        public async Task VerifyTokenResetPassword_Throws_WhenUserInactive()
        {
            var user = CreateTestUser((int)UserStatus.Inactive);
            var token = new PasswordResetToken { UserId = user.Id, IsUsed = false, ExpireAt = DateTime.UtcNow.AddMinutes(10) };

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                               .ReturnsAsync(token);
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyTokenResetPassword("some_token"));

            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPassword_ReturnsTrue_WhenValid()
        {
            // Arrange
            var user = CreateTestUser(); // helper method to get a valid user
            var token = new PasswordResetToken
            {
                Token = "valid_token",
                UserId = user.Id,
                IsUsed = false,
                ExpireAt = DateTime.UtcNow.AddMinutes(10)
            };

            var resetPasswordDto = new ResetPasswordDTO
            {
                ResetPasswordToken = "valid_token",
                Password = "new_password"
            };

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                       .ReturnsAsync(token);

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            _commonServiceMock.Setup(c => c.Hash("new_password"))
                              .Returns("hashed_new_password");

            _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

            _passwordResetTokenRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>()))
                               .ReturnsAsync(new List<PasswordResetToken> { token });

            _passwordResetTokenRepoMock.Setup(r => r.DeleteRangeAsync(It.IsAny<List<PasswordResetToken>>()))
                                       .Returns(Task.CompletedTask);

            var service = CreateService();

            // Act
            var result = await service.ResetPassword(resetPasswordDto);

            // Assert
            Assert.True(result);
            Assert.True(token.IsUsed);
            Assert.Equal("hashed_new_password", user.Password);
        }

        [Fact]
        public async Task ResetPassword_Throws_WhenDtoInvalid()
        {
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(null!));

            Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex.Message);
        }
    }
}