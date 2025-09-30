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
using FluentAssertions;
using AutoMapper;

namespace QuizVerse.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ICommonService> _commonServiceMock = new();
        private readonly Mock<IGenericRepository<User>> _userRepoMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();
        private readonly Mock<IGenericRepository<PasswordResetToken>> _passwordResetTokenRepoMock = new();
        private readonly Mock<IUserService> _userServiceMock = new();

        private AuthService CreateService() =>
            new(_tokenServiceMock.Object, _commonServiceMock.Object, _userRepoMock.Object,
                _passwordResetTokenRepoMock.Object, _mapperMock.Object, _configurationMock.Object,
                _userServiceMock.Object);

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IGenericRepository<User>>();
            _tokenServiceMock = new Mock<ITokenService>();

            // Ensure test email template file exists
            Directory.CreateDirectory("TestTemplates");
            File.WriteAllText(Path.Combine("TestTemplates", "WelcomeEmail.html"),
                "<html>Welcome {{userEmail}} on {{registrationDate}}</html>");
        }

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

        private UserPerformanceDetail CreatePerfDetails(int userId, int currentStreak = 0, int highestStreak = 0, DateTime? modifiedDate = null)
        {
            return new UserPerformanceDetail
            {
                UserId = userId,
                CurrentStreak = currentStreak,
                HighestStreak = highestStreak,
                ModifiedDate = modifiedDate
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

        #region AuthenticateUser/Login
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
        #endregion

        #region ValidateRefreshTokens
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
        private static UserRegisterDto CreateValidUserDto() => new()
        {
            FullName = "Jane Doe",
            Email = "jane@example.com",
            UserName = "janedoe",
            Password = "SecurePass123",
            Bio = "Tester at QuizVerse"
        };
        #endregion

        #region RegisterUser
        [Fact]
        public async Task RegisterUser_Should_CallMapper_And_UserService()
        {
            // Arrange
            var userRegisterDto = new UserRegisterDto { Email = "test@example.com", Password = "pass" };
            var mappedRequestDto = new UserRequestDto { Email = userRegisterDto.Email, IsRegister = true };

            _mapperMock.Setup(m => m.Map<UserRequestDto>(userRegisterDto))
                       .Returns(mappedRequestDto);

            _userServiceMock.Setup(u => u.CreateOrUpdateUser(mappedRequestDto, false))
                            .ReturnsAsync((true, "User created successfully."));

            var service = CreateService();

            // Act
            var (success, message) = await service.RegisterUser(userRegisterDto);

            // Assert
            success.Should().BeTrue();
            message.Should().Be("User created successfully.");

            _mapperMock.Verify(m => m.Map<UserRequestDto>(userRegisterDto), Times.Once);
            _userServiceMock.Verify(u => u.CreateOrUpdateUser(mappedRequestDto, false), Times.Once);
        }

        [Fact]
        public async Task RegisterUser_Should_ReturnFailure_When_UserServiceFails()
        {
            // Arrange
            var userRegisterDto = new UserRegisterDto { Email = "fail@example.com", Password = "pass" };
            var mappedRequestDto = new UserRequestDto { Email = userRegisterDto.Email, IsRegister = true };

            _mapperMock.Setup(m => m.Map<UserRequestDto>(userRegisterDto))
                       .Returns(mappedRequestDto);

            _userServiceMock.Setup(u => u.CreateOrUpdateUser(mappedRequestDto, false))
                            .ReturnsAsync((false, "Failed to create user"));

            var service = CreateService();

            var (success, message) = await service.RegisterUser(userRegisterDto);
            success.Should().BeFalse();
            message.Should().Be("Failed to create user");
        }
        #endregion

        #region ForgotPassword
        [Fact]
        public async Task ForgotPassword_ReturnsTrue_WhenValidUser()
        {
            // Arrange
            var testEmail = "test@example.com";
            var testUser = new User
            {
                Id = 1,
                Email = testEmail,
                FullName = "Test User",
                Status = (int)UserStatus.Active,
                IsDeleted = false
            };

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(testUser);

            _tokenServiceMock.Setup(t => t.GenerateSecureToken(32))
                 .Returns("securetoken123");

            _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
                                      .Returns(Task.CompletedTask)
                                      .Callback<PasswordResetToken>(token => token.TokenId = 1);

            _configurationMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"])
                              .Returns("30");
            _configurationMock.Setup(c => c["baseUrl"])
                              .Returns("https://example.com");

            _commonServiceMock.Setup(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
                              .ReturnsAsync((TemplatedEmailRequestDto dto) =>
                                  string.Format(Constants.EMAIL_SENT_SUCCESS, dto.ToEmail));

            var service = CreateService();

            // Act
            var result = await service.ForgotPassword(testEmail);

            // Assert
            result.Should().BeTrue();
            _commonServiceMock.Verify(s => s.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()), Times.Once);
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
            _configurationMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"]).Returns("15");
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
            _userRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateSecureToken(32)).Returns("secure_token");
            _configurationMock.Setup(c => c["ResetPasswordTokenExpiryMinutes"]).Returns("30");
            _configurationMock.Setup(c => c["baseUrl"]).Returns("https://example.com");
            _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
                                       .Callback<PasswordResetToken>(t => t.TokenId = 1)
                                       .Returns(Task.CompletedTask);
            _commonServiceMock
                .Setup(c => c.SendEmailFromTemplate(It.IsAny<TemplatedEmailRequestDto>()))
                .ReturnsAsync("Failed to send email");

            var service = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => service.ForgotPassword(user.Email));
            Assert.Equal(Constants.EMAIL_NOT_SENT, ex.Message);
        }
        #endregion

        #region VerifyTokenResetPassword
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
        #endregion

        #region ResetPassword
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
        #endregion

        #region IsUserNameAvailable
        [Fact]
        public async Task IsUserNameAvailable_ShouldReturnTrue_WhenUserNameDoesNotExist()
        {
            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(false);

            AuthService service = CreateService();

            bool result = await service.IsUserNameAvailable("NewUser");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsUserNameAvailable_ShouldThrow_WhenUserNameExists_ExactMatch()
        {
            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(true);

            AuthService service = CreateService();

            Func<Task<bool>> act = async () => await service.IsUserNameAvailable("ExistingUser");

            await act.Should().ThrowAsync<AppException>()
                .WithMessage(Constants.DUPLICATE_USERNAME);
        }

        [Fact]
        public async Task IsUserNameAvailable_ShouldAllow_WhenSameUserIdUpdating()
        {
            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(false);

            AuthService service = CreateService();

            bool result = await service.IsUserNameAvailable("ExistingUser", id: 1);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsUserNameAvailable_ShouldBeCaseSensitive()
        {
            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(false);

            AuthService service = CreateService();

            bool result = await service.IsUserNameAvailable("john");

            result.Should().BeTrue();
        }
        #endregion

        #region IsEmailAvailable
        [Fact]
        public async Task IsEmailAvailable_ShouldReturnTrue_WhenEmailNotFound()
        {
            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync((User)null);

            var service = CreateService();

            var result = await service.IsEmailAvailable("new@example.com");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsEmailAvailable_ShouldThrow_WhenEmailIsSuspended()
        {
            var user = CreateTestUser(status: (int)UserStatus.Suspended);

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            var service = CreateService();

            Func<Task> act = async () => await service.IsEmailAvailable(user.Email);

            await act.Should().ThrowAsync<AppException>()
                .WithMessage(Constants.EMAIL_SUSPENDED);
        }

        [Fact]
        public async Task IsEmailAvailable_ShouldReturnTrue_WhenUserIsDeleted()
        {
            var user = CreateTestUser(status: (int)UserStatus.Active);
            user.IsDeleted = true;

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            var service = CreateService();

            var result = await service.IsEmailAvailable(user.Email);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsEmailAvailable_ShouldThrow_WhenUserIsActiveAndNotDeleted()
        {
            var user = CreateTestUser(status: (int)UserStatus.Active);
            user.IsDeleted = false;

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            var service = CreateService();

            Func<Task> act = async () => await service.IsEmailAvailable(user.Email);

            await act.Should().ThrowAsync<AppException>()
                .WithMessage(Constants.EMAIL_ALREADY_IN_USE);
        }

        [Fact]
        public async Task IsEmailAvailable_ShouldThrow_WhenUserStatusIsUnknown()
        {
            var user = CreateTestUser(status: 99); // Unknown status
            user.IsDeleted = false;

            _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                         .ReturnsAsync(user);

            var service = CreateService();

            Func<Task> act = async () => await service.IsEmailAvailable(user.Email);

            await act.Should().ThrowAsync<AppException>()
                .WithMessage(Constants.EMAIL_ALREADY_IN_USE);
        }

        #endregion

    }
}