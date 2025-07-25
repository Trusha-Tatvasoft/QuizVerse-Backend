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

namespace QuizVerse.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ICustomService> _customServiceMock = new();
        private readonly Mock<IGenericRepository<User>> _userRepoMock = new();

        private AuthService CreateService() =>
            new AuthService(_tokenServiceMock.Object, _customServiceMock.Object, _userRepoMock.Object);

        private User CreateTestUser(int status = (int)UserStatus.Active)
        {
            return new User
            {
                Id = 1,
                Email = "test@example.com",
                Password = "$2a$10$7HPBHeXOqGMn.fDW9mvpaeolxiGERa5xcV1y1Nd66wubh3ee86coW",
                Status = status,
                Role = new Domain.Entities.UserRole { Id = 2, Name = "Player" }
            };
        }

        [Fact]
        public async Task AuthenticateUser_ReturnsTokens_WhenCredentialsAreValid()
        {
            // Arrange
            var userDto = new UserLoginDTO
            {
                Email = "test@example.com",
                Password = "Hsrad@123",
                RememberMe = false
            };

            var user = CreateTestUser();

            _userRepoMock.Setup(r =>
                r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                .ReturnsAsync(user);

            _customServiceMock.Setup(s => s.VerifyPassword(userDto.Password, user.Password))
                .Returns(true);

            _tokenServiceMock.Setup(t => t.GenerateAccessTokenAsync(user))
                .Returns("mock_access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshTokenAsync(user, userDto.RememberMe))
                .Returns("mock_refresh_token");

            var service = CreateService();

            // Act
            var (accessToken, refreshToken) = await service.AuthenticateUser(userDto);

            // Assert
            Assert.Equal("mock_access_token", accessToken);
            Assert.Equal("mock_refresh_token", refreshToken);

            _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.LastLogin != null)), Times.Once);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenUserLoginDtoIsNull()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(null!));
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenEmailIsInvalid()
        {
            var service = CreateService();
            var dto = new UserLoginDTO { Email = "", Password = "Hsrad@123" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenUserNotFound()
        {
            _userRepoMock.Setup(r =>
                r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                .ReturnsAsync((User?)null);

            var service = CreateService();

            var dto = new UserLoginDTO { Email = "test@example.com", Password = "Hsrad@123" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Equal(Constants.USER_NOT_FOUND_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenPasswordIsInvalid()
        {
            var user = CreateTestUser();

            _userRepoMock.Setup(r =>
                r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                .ReturnsAsync(user);

            _customServiceMock.Setup(s => s.VerifyPassword("Hsrad@123", user.Password))
                .Returns(false);

            var service = CreateService();

            var dto = new UserLoginDTO { Email = "test@example.com", Password = "Hsrad@123" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Equal(Constants.INVALID_PASSWORD_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WhenUserIsInactive()
        {
            var user = CreateTestUser(status: (int)UserStatus.Inactive);

            _userRepoMock.Setup(r =>
                r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                .ReturnsAsync(user);

            var service = CreateService();

            var dto = new UserLoginDTO { Email = "test@example.com", Password = "Hsrad@123" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Equal(Constants.INACTIVE_USER_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task AuthenticateUser_Throws_WithRemainingSuspensionTime_WhenUserIsSuspended()
        {
            var user = CreateTestUser(status: (int)UserStatus.Suspended);
            user.ModifiedDate = DateTime.UtcNow.AddDays(-10);

            _userRepoMock.Setup(r =>
                r.GetAsync(It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                .ReturnsAsync(user);

            var service = CreateService();

            var dto = new UserLoginDTO { Email = "test@example.com", Password = "Hsrad@123" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AuthenticateUser(dto));
            Assert.Contains("You have been suspended", ex.Message);
            Assert.Contains("Remaining suspension time", ex.Message);
        }
    }
}
