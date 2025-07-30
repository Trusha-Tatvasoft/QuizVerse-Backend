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

        #region ResetPasswordTokenValidation Tests
        [Fact]
        public async Task ResetPasswordTokenValidation_Throws_WhenTokenIsEmpty()
        {
            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordTokenValidation(""));
            Assert.Equal(Constants.EMPTY_TOKEN_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordTokenValidation_Throws_WhenTokenInvalid()
        {
            _tokenServiceMock.Setup(t => t.ValidateToken("invalid_token", true))
                            .Returns((ClaimsPrincipal)null!);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordTokenValidation("invalid_token"));
            Assert.Equal(Constants.INVALID_RESET_PASSWORD_TOKEN, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordTokenValidation_Throws_WhenUserIdInvalid()
        {
            var principal = CreateClaimsPrincipal(1);
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("notanint");

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordTokenValidation("token"));
            Assert.Equal(Constants.INVALID_USER_ID_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordTokenValidation_Throws_WhenTokenNotFoundOrInvalid()
        {
            var principal = CreateClaimsPrincipal(1);
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync((PasswordResetToken?)null);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordTokenValidation("token"));
            Assert.Equal(Constants.INVALID_RESET_PASSWORD_TOKEN, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordTokenValidation_Throws_WhenUserNotFound()
        {
            var principal = CreateClaimsPrincipal(1);
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync((User?)null);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordTokenValidation("token"));
            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordTokenValidation_Throws_WhenUserInactive()
        {
            var principal = CreateClaimsPrincipal(1);
            var user = CreateTestUser((int)UserStatus.Inactive);
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordTokenValidation("token"));
            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordTokenValidation_ReturnsTrue_WhenValid()
        {
            var principal = CreateClaimsPrincipal(1);
            var user = CreateTestUser();
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };

            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);

            var service = CreateService();

            var result = await service.ResetPasswordTokenValidation("token");

            Assert.True(result);
        }
        #endregion

        #region ResetPassword Tests
        [Fact]
        public async Task ResetPassword_Throws_WhenDtoIsNullOrInvalid()
        {
            var service = CreateService();

            var ex1 = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(null!));
            var ex2 = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(new ResetPasswordDTO { ResetPasswordToken = "", Password = "newpass" }));
            var ex3 = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(new ResetPasswordDTO { ResetPasswordToken = "token", Password = "" }));

            Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex1.Message);
            Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex2.Message);
            Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex3.Message);
        }

        [Fact]
        public async Task ResetPassword_Throws_WhenTokenNotFoundOrInvalid()
        {
            var dto = new ResetPasswordDTO { ResetPasswordToken = "token", Password = "newpass" };
            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync((PasswordResetToken?)null);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(dto));
            Assert.Equal(Constants.INVALID_RESET_PASSWORD_TOKEN, ex.Message);
        }

        [Fact]
        public async Task ResetPassword_Throws_WhenTokenPrincipalInvalid()
        {
            var dto = new ResetPasswordDTO { ResetPasswordToken = "token", Password = "newpass" };
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns((ClaimsPrincipal)null!);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(dto));
            Assert.Equal(Constants.INVALID_RESET_PASSWORD_TOKEN, ex.Message);
        }

        [Fact]
        public async Task ResetPassword_Throws_WhenUserIdInvalid()
        {
            var dto = new ResetPasswordDTO { ResetPasswordToken = "token", Password = "newpass" };
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };
            var principal = CreateClaimsPrincipal(1);

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("notanint");

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(dto));
            Assert.Equal(Constants.INVALID_USER_ID_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPassword_Throws_WhenUserNotFound()
        {
            var dto = new ResetPasswordDTO { ResetPasswordToken = "token", Password = "newpass" };
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };
            var principal = CreateClaimsPrincipal(1);

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync((User?)null);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(dto));
            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPassword_Throws_WhenUserInactive()
        {
            var dto = new ResetPasswordDTO { ResetPasswordToken = "token", Password = "newpass" };
            var user = CreateTestUser((int)UserStatus.Inactive);
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };
            var principal = CreateClaimsPrincipal(1);

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPassword(dto));
            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPassword_ReturnsTrue_WhenSuccessful()
        {
            var dto = new ResetPasswordDTO { ResetPasswordToken = "token", Password = "newpass" };
            var user = CreateTestUser();
            var resetToken = new PasswordResetToken
            {
                TokenId = 1,
                Token = "token",
                UserId = 1,
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };
            var principal = CreateClaimsPrincipal(1);

            _passwordResetTokenRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PasswordResetToken, bool>>>(), null))
                                      .ReturnsAsync(resetToken);
            _tokenServiceMock.Setup(t => t.ValidateToken("token", true)).Returns(principal);
            _tokenServiceMock.Setup(t => t.GetUserIdFromToken(principal)).Returns("1");
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                         .ReturnsAsync(user);
            _commonServiceMock.Setup(c => c.Hash(dto.Password)).Returns("hashed_newpass");
            _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
            _passwordResetTokenRepoMock.Setup(r => r.UpdateAsync(resetToken)).Returns(Task.CompletedTask);

            var service = CreateService();

            var result = await service.ResetPassword(dto);

            Assert.True(result);
            Assert.Equal("hashed_newpass", user.Password);
            Assert.True(resetToken.IsUsed);
        }
        #endregion

        #region ResetPasswordMail Tests
        [Fact]
        public async Task ResetPasswordMail_Throws_WhenUserNotFound()
        {
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync((User?)null);
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<AppException>(() => service.ResetPasswordMail("unknown@test.com"));
            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordMail_Throws_WhenUserInactive()
        {
            var user = CreateTestUser((int)UserStatus.Inactive);
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordMail(user.Email));
            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordMail_Throws_WhenTokenCreationFails()
        {
            var user = CreateTestUser();
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateResetPasswordToken(user)).Returns("token");
            _configMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"]).Returns("15");

            var passwordResetToken = new PasswordResetToken { TokenId = 0, Token = "" }; // Simulate failure
            _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
                                              .Callback<PasswordResetToken>(t => t.TokenId = 0);
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<AppException>(() => service.ResetPasswordMail(user.Email));
            Assert.Equal(Constants.FAILED_TO_CREATE_RESET_PASSWORD_TOKEN, ex.Message);
        }

        [Fact]
        public async Task ResetPasswordMail_ReturnsTrue_WhenEmailSentSuccessfully()
        {
            var user = CreateTestUser();
            var token = "resettoken";

            string tempDir = Path.Combine(Path.GetTempPath(), "QuizVerseTests");
            Directory.CreateDirectory(tempDir);

            string tempTemplatePath = Path.Combine(tempDir, "ResetPassword.html");

            await File.WriteAllTextAsync(tempTemplatePath, @"
                    <html>
                        <body>
                            Hello {userName}, reset your password using this link: {resetLink}
                        </body>
                    </html>
                ");

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            _tokenServiceMock.Setup(t => t.GenerateResetPasswordToken(user)).Returns(token);
            _configMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"]).Returns("15");
            _configMock.Setup(c => c["baseUrl"]).Returns("https://example.com");
            _configMock.Setup(c => c["EmailSettings:ResetPasswordTemplatePath"]).Returns(tempTemplatePath);

            var passwordResetToken = new PasswordResetToken { TokenId = 1, Token = token };
            _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
                                       .Callback<PasswordResetToken>(t =>
                                       {
                                           t.TokenId = 1;
                                           t.Token = token;
                                       }).Returns(Task.CompletedTask);

            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>()))
                             .ReturnsAsync(true);

            var service = CreateService();

            try
            {
                var result = await service.ResetPasswordMail(user.Email);

                Assert.True(result);
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempTemplatePath))
                    File.Delete(tempTemplatePath);
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
        #endregion
    }
}
