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
        private readonly Mock<ICommonService> _customServiceMock = new();
        private readonly Mock<IGenericRepository<User>> _userRepoMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        private AuthService CreateService() =>
            new(_tokenServiceMock.Object, _customServiceMock.Object, _userRepoMock.Object, _emailServiceMock.Object, _configurationMock.Object, _mapperMock.Object);


        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IGenericRepository<User>>();
            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            _tokenServiceMock = new Mock<ITokenService>();

            _configurationMock.Setup(c => c["EmailSettings:TemplatePath"]).Returns("TestTemplates");

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
            _customServiceMock.Setup(s => s.VerifyPassword(userDto.Password, user.Password)).Returns(true);
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
            _customServiceMock.Setup(s => s.VerifyPassword(dto.Password, user.Password)).Returns(true);
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
            _customServiceMock.Setup(s => s.VerifyPassword(dto.Password, user.Password)).Returns(false);

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
            _customServiceMock.Setup(s => s.VerifyPassword(dto.Password, user.Password)).Returns(true);
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
        private static UserRegisterDto CreateValidUserDto() => new()
        {
            FullName = "Jane Doe",
            Email = "jane@example.com",
            UserName = "janedoe",
            Password = "SecurePass123",
            Bio = "Tester at QuizVerse"
        };

        [Fact]
        public async Task RegisterUser_Should_Throw_When_EmailAlreadyExists()
        {
            var userRegisterDto = CreateValidUserDto();

            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(true);

            var service = CreateService();

            var act = async () => await service.RegisterUser(userRegisterDto);

            await act.Should().ThrowAsync<AppException>().WithMessage(Constants.DUPLICATE_EMAIL);
        }


        [Fact]
        public async Task RegisterUser_Should_Throw_When_UsernameAlreadyExists()
        {
            var userRegisterDto = CreateValidUserDto();

            _userRepoMock.SetupSequence(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(false) // Email does not exist
                .ReturnsAsync(true); // Username exists

            var service = CreateService();

            var act = async () => await service.RegisterUser(userRegisterDto);

            await act.Should().ThrowAsync<AppException>().WithMessage(Constants.DUPLICATE_USERNAME);
        }


        [Fact]
        public async Task RegisterUser_Should_Throw_When_TemplatePathMissing()
        {
            var userRegisterDto = CreateValidUserDto();

            _configurationMock.Setup(c => c["EmailSettings:RegisterUserTemplatePath"]).Returns((string?)null);

            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
            _customServiceMock.Setup(s => s.Hash(It.IsAny<string>())).Returns("hashedPassword");
            _mapperMock.Setup(m => m.Map<User>(It.IsAny<UserRegisterDto>())).Returns(new User { CreatedDate = DateTime.UtcNow });

            var service = CreateService();

            var act = async () => await service.RegisterUser(userRegisterDto);

            await act.Should().ThrowAsync<AppException>().WithMessage(Constants.EMAIL_PATH_NOT_CONFIGURED);
        }

        [Fact]
        public async Task RegisterUser_Should_Throw_When_TemplateFileMissing()
        {
            var userRegisterDto = CreateValidUserDto();

            string path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Templates", "Welcome.html");
            if (File.Exists(path))
                File.Delete(path);

            _configurationMock.Setup(c => c["EmailSettings:RegisterUserTemplatePath"]).Returns("TestData/Templates/Welcome.html");
            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
            _customServiceMock.Setup(s => s.Hash(It.IsAny<string>())).Returns("hashedPassword");
            _mapperMock.Setup(m => m.Map<User>(It.IsAny<UserRegisterDto>())).Returns(new User { CreatedDate = DateTime.UtcNow });

            var service = CreateService();

            var act = async () => await service.RegisterUser(userRegisterDto);

            await act.Should().ThrowAsync<AppException>().WithMessage(Constants.EMAIL_PATH_NOT_CONFIGURED);
        }


        [Fact]
        public async Task RegisterUser_Should_ReturnTrue_WhenEmailSentSuccessfully()
        {
            var userRegisterDto = CreateValidUserDto();

            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Templates");
            Directory.CreateDirectory(directoryPath);
            string templatePath = Path.Combine(directoryPath, "Welcome.html");
            await File.WriteAllTextAsync(templatePath, "Welcome {{userEmail}}!");

            _configurationMock.Setup(c => c["EmailSettings:RegisterUserTemplatePath"]).Returns("TestData/Templates/Welcome.html");
            _userRepoMock.Setup(r => r.Exists(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
            _customServiceMock.Setup(s => s.Hash(It.IsAny<string>())).Returns("hashedPassword");

            var mappedUser = new User
            {
                Email = userRegisterDto.Email,
                CreatedDate = DateTime.UtcNow
            };

            _mapperMock.Setup(m => m.Map<User>(It.IsAny<UserRegisterDto>())).Returns(mappedUser);
            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>())).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterUser(userRegisterDto);

            result.success.Should().BeTrue();
            result.message.Should().Contain(Constants.EMAIL_SENT_SUCCESS);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>()), Times.Once);
        }
    }
}
